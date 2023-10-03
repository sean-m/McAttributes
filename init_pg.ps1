#Requires -Version 5
<#
.Synopsis
   Short description
.DESCRIPTION
   Long description
.EXAMPLE
   Example of how to use this cmdlet
.EXAMPLE
   Another example of how to use this cmdlet
#>
begin {
    Push-Location (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition)

}
process {

    #<#
    docker stop postgresql_database

    docker run --name postgresql_database `
        --memory 512M `
        --rm `
        -d `
        -e POSTGRES_USER=pguser `
        -e POSTGRES_PASSWORD=egm7DfeK `
        -e POSTGRESQL_DATABASE=identity `
        -p 5432:5432 `
        docker.io/library/postgres:latest
        #-v "$((pwd).Path))/data":/var/lib/postgresql/data `
        #>

    $sqlQueryCreateDB = @"
CREATE DATABASE identity
    WITH
    OWNER = pguser
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;
"@ 

    $sqlQuery = @"
create table if not exists graphraw (
	id bigserial primary key,
	created timestamp,
	dataSourceId varchar(128),
	tenant text,
	graphRecord jsonb
);
CREATE INDEX IF NOT EXISTS graphraw_created_idx ON graphraw (created);
CREATE INDEX IF NOT EXISTS graphraw_datasourceid_idx ON graphraw (datasourceid);

create table if not exists hr (
	id serial primary key,
	created timestamp,
	hrRecord jsonb
);

create table if not exists dataLog (
	id serial primary key,
	created timestamp DEFAULT NOW(),
	logType varchar(64),
	message varchar(1024),
	file varchar(1024),
	jobId varchar(64)
);

create table if not exists wdusers (
    id serial primary key,
    created timestamp,
    modified text,
    tenant text,
    aadId text,
    employeeId TEXT unique,
    wdEmployeeId text
);

CREATE TABLE if NOT EXISTS azusers (
 id BIGSERIAL PRIMARY KEY,
 merged TIMESTAMP,
 modified TIMESTAMP,
 lastFetched TIMESTAMP,
 created TIMESTAMP,
 enabled BOOLEAN,
 onPremisesSyncEnabled BOOLEAN,
 tenant TEXT,
 aadId UUID UNIQUE,
 refersTo UUID,
 upn TEXT,
 mail TEXT,
 employeeId TEXT,
 adEmployeeId TEXT,
 hrEmployeeId TEXT,
 wid TEXT,
 creationType TEXT,
 company TEXT,
 displayName TEXT,
 preferredGivenName TEXT,
 preferredSurname TEXT,
 articles TEXT,
 pronouns TEXT,
 otherMails varchar(256)[]
);
CREATE INDEX IF NOT EXISTS idx_azusers_aadid ON azusers(aadId);
CREATE INDEX IF NOT EXISTS idx_azusers_mail ON azusers(mail);
CREATE INDEX IF NOT EXISTS idx_azusers_empId ON azusers(employeeId);


/*
	Tables to support employeeId syncing.
*/
CREATE TABLE IF NOT EXISTS employeeIdJoin (
	id SERIAL PRIMARY KEY,
	employeeId VARCHAR(24),
	upn VARCHAR(256),
	aadId UUID UNIQUE
);
CREATE INDEX IF NOT EXISTS idx_empid_empid ON employeeIdJoin(employeeId);
CREATE INDEX IF NOT EXISTS idx_empid_upn ON employeeIdJoin(upn);

CREATE TABLE IF NOT EXISTS idAlerts (
	id SERIAL PRIMARY KEY,
	created TIMESTAMP DEFAULT NOW(),
	attrName VARCHAR(64),
	alertHash VARCHAR(64) UNIQUE,
	created TIMESTAMP,
	status VARCHAR(64),
	description TEXT,
	notes TEXT
);
CREATE INDEX IF NOT EXISTS idx_alerts_hash ON idAlerts(alertHash);


/*
	Tables to support Azure group syncing.
*/
CREATE TABLE IF NOT EXISTS azgroups (
	id SERIAL PRIMARY KEY,
	merged TIMESTAMP,
	modified TIMESTAMP,
	lastFetched TIMESTAMP,
	created TIMESTAMP,
	displayName TEXT,
	tenant TEXT,
	aadId UUID UNIQUE
);

CREATE TABLE IF NOT EXISTS azgroupmembers (
	id 				SERIAL PRIMARY KEY,
	partition 		VARCHAR(64) NOT NULL,
	azgroupId 		VARCHAR(40) NOT NULL,
	azgroupName 	VARCHAR(256) NOT NULL,
	memberId 		VARCHAR(256) NOT NULL,
	memberGlobalId INTEGER,
	memberType 		VARCHAR(12),
	UNIQUE (azgroupId, memberId)
);

CREATE INDEX IF NOT EXISTS idx_azgrpmember_groupId ON azgroupmembers (azgroupid);
CREATE INDEX IF NOT EXISTS idx_azgrpmember_groupName ON azgroupmembers (azgroupName);
CREATE INDEX IF NOT EXISTS idx_azgrpmember_groupmember ON azgroupmembers (memberId);


CREATE TABLE IF NOT EXISTS azgroupmembersimport (
	id 			SERIAL PRIMARY KEY,
	partition 	VARCHAR(64) NOT NULL,
	azgroupId 	VARCHAR(40) NOT NULL,
	azgroupName VARCHAR(256) NOT NULL,
	memberId 	UUID NOT NULL,
	memberType 	VARCHAR(12),
	UNIQUE (azgroupId, memberId)
);

CREATE INDEX IF NOT EXISTS idx_azgroupmembersimport_partition ON azgroupmembersimport (partition);
CREATE INDEX IF NOT EXISTS idx_azgroupmembersimport_groupId ON azgroupmembersimport (azgroupid);
CREATE INDEX IF NOT EXISTS idx_azgroupmembersimport_groupName ON azgroupmembersimport (azgroupName);
CREATE INDEX IF NOT EXISTS idx_azgroupmembersimport_groupmember ON azgroupmembersimport (memberId);


/*
	Identity crosswalk table.
*/
/*
enum recordType {
	prime=0,
	user=1,
	priveleged=2,
	group=3
}
*/
CREATE TABLE IF NOT EXISTS stargate (
	id BIGSERIAL PRIMARY KEY,
	recordType INTEGER,
	globalId BIGINT,
	localId VARCHAR(256) NOT NULL UNIQUE,
	upn VARCHAR(256),
	partition VARCHAR(256),
	joinSeed VARCHAR(256) UNIQUE,
	
	CONSTRAINT globalId_fk 
		FOREIGN KEY (globalId)
		REFERENCES stargate(id)
);
CREATE INDEX IF NOT EXISTS idx_sg_joinSeed ON stargate(joinSeed);
CREATE INDEX IF NOT EXISTS idx_sg_localId ON stargate(localId);
CREATE INDEX IF NOT EXISTS idx_sg_globalId ON stargate(localId);
CREATE INDEX IF NOT EXISTS idx_sg_upn ON stargate(upn);


CREATE OR REPLACE FUNCTION partitionCrosswalk(TEXT, TEXT) RETURNS TABLE (aadId TEXT)
AS `$$
	SELECT SG1.localId FROM stargate SG1
	JOIN stargate AS mapped
	ON mapped.globalId = SG1.globalId
		AND mapped.localId = `$1
		AND SG1.partition = LOWER(TRIM(`$2))
`$$
LANGUAGE 'sql'
;

CREATE OR REPLACE FUNCTION partitionCrosswalk(INTEGER, TEXT) RETURNS TABLE (aadId TEXT)
AS `$$
	SELECT localId FROM stargate
	WHERE globalId = `$1
	AND partition = LOWER(TRIM(`$2))
`$$
LANGUAGE 'sql'
;

CREATE OR REPLACE FUNCTION partitionCrosswalkByMail(TEXT, TEXT) RETURNS TABLE (aadId TEXT)
AS `$$
	SELECT aadId FROM partitionCrosswalk((SELECT aadId FROM azusers WHERE mail = `$1 LIMIT 1)::TEXT, `$2)
`$$
LANGUAGE 'sql'
;


CREATE OR REPLACE FUNCTION getAzuserFromPartitionCrosswalk(TEXT, TEXT) RETURNS TABLE (aadId TEXT, displayName TEXT, upn TEXT, employeeId TEXT)
AS `$$
	SELECT aadId, displayName, upn, employeeId FROM azusers az
	WHERE aadId = (SELECT aadId FROM partitioncrosswalk(`$1, `$2))::UUID
`$$
LANGUAGE 'sql'
;

CREATE OR REPLACE FUNCTION getAzuserFromPartitionCrosswalkByMail(TEXT, TEXT) RETURNS TABLE (aadId TEXT, displayName TEXT, upn TEXT, employeeId TEXT)
AS `$$
	SELECT aadId, displayName, upn, employeeId FROM azusers az
	WHERE aadId = (SELECT aadId FROM partitioncrosswalkByMail(LOWER(`$1), `$2))::UUID
`$$
LANGUAGE 'sql'
;



DROP FUNCTION IF EXISTS objIdToGlobalId (VARCHAR(256));
CREATE OR REPLACE FUNCTION objIdToGlobalId (VARCHAR(256)) RETURNS INTEGER
AS `$$
	SELECT globalId FROM stargate WHERE localId = `$1
`$$
LANGUAGE 'sql'
;

DROP FUNCTION IF EXISTS objIdToGlobalIdAndType (VARCHAR(256));
CREATE OR REPLACE FUNCTION objIdToGlobalIdAndType (VARCHAR(256)) RETURNS TABLE (globalId INTEGER, recordType INTEGER)
AS `$$
	SELECT globalId, recordType FROM stargate WHERE localId = `$1
`$$
LANGUAGE 'sql'
;



/*
 *   Group Tables and Functions
 */

--drop table if exists FederatedGroup cascade;
create table if not exists FederatedGroup (
	id BIGSERIAL primary key
	,displayName varchar(256) unique
);

--drop table if exists FederatedGroupMembership cascade;
create table if not exists FederatedGroupMembership (
	id BIGSERIAL primary key
	,groupId BIGSERIAL
	,globalId BIGSERIAL
	
	,CONSTRAINT globalId_fk 
		FOREIGN KEY (globalId)
		REFERENCES stargate(id)
		
	,constraint groupId_fk
		foreign key (groupId)
		references FederatedGroup(id)
		on delete cascade
);
create unique index if not exists groupId_globalId_uniq on FederatedGroupMembership(groupId, globalId);

--drop table if exists LocalGroup cascade;
create table if not exists LocalGroup (
	id BIGSERIAL primary key
	,groupId BIGSERIAL
	,localId varchar(256) unique
	,partition varchar(64)
	
	,constraint groupId_fk
		foreign key (groupId)
		references FederatedGroup(id)
		on delete cascade
);

--drop table if exists LocalGroupMembership cascade;
create table if not exists LocalGroupMembership (
	id BIGSERIAL primary key
	,localGroupId BIGSERIAL
	,globalId BIGSERIAL
	,globalMemberType integer
	,localId varchar(256)
	,partition varchar(64)
	,localOnly boolean default false
	
	,CONSTRAINT globalId_fk 
		FOREIGN KEY (globalId)
		REFERENCES stargate(id)
		
	,constraint localGroup_fk
		foreign key (localGroupId)
		references LocalGroup(id)
		on delete cascade
);
create unique index if not exists localGroup_localId_uniq on LocalGroupMembership(localGroupId, localId);
 
DROP FUNCTION IF EXISTS getNeededStandardGroupMemberChanges(GroupName VARCHAR(256), Parititon VARCHAR(256));
CREATE OR REPLACE FUNCTION getNeededStandardGroupMemberChanges(GroupName VARCHAR(256), Partition VARCHAR(256)) RETURNS TABLE (groupId TEXT, memberId TEXT, upn VARCHAR(256), partition VARCHAR(256), change VARCHAR(256)) --, globalMemberType integer)
AS `$$
	-- Get list of federated group memberships for the specified group name.
	-- Federated member records can only be contributed by a native account
	-- in one of the contributing tenants, B2B accounts or 'guests' do not
	-- contribute to the federated member set by convention. 
	with recursive FederatedMembers as (
		select fgm.* from FederatedGroupMembership as fgm
		join FederatedGroup as fg
		on fgm.groupId = fg.id 
		where fg.displayname = `$1
	),
	SelectedLocalgroup as (
		select id, groupid, localid, partition from LocalGroup 
	   where groupId = (select groupid from FederatedMembers limit 1) 
	   and partition = `$2
	   limit 1
	),
	NativeLocalMembers as (
		-- Get native and guest member records and join that with the federated members.
		select lgm.* from LocalGroupMembership as lgm
		join SelectedLocalGroup as lg
		on lgm.localGroupId = lg.id
			and lgm.partition = `$2
		where lgm.globalmembertype < 30	
	),
	MembershipChanges as (
		-- Get list of federated group memberships for the specified group name.
		-- Federated member records can only be contributed by a native account
		-- in one of the contributing tenants, B2B accounts or 'guests' do not
		-- contribute to the federated member set by convention.
		select case when fm.globalId is null then lgm.globalid else fm.globalId end as globalId -- Capture the global id of the member in question.
			,case when fm.globalId is null then 'remove' else 'add' end as "action"           -- Use null globalId values in the federated or local member set to infer an add or remove action.
			                                                                                  -- Membership records that exist in both sets are not considered. No changes needed.
			,lgm.globalmembertype
		from FederatedMembers as fm
		full join NativeLocalMembers as lgm
		on fm.globalid = lgm.globalid
		where (fm.globalId is null
			or lgm.globalid is null)
	),
	LocalMappedMembers as (
		/*
		Join local group member records with stargate as the local member
		records are the stargate record local to the partition, they don't
		carry the reference to an account's prime record which is needed
		when comparing with the federated member record.
		
		Foo user account with guests in Bar and Baz mapped through
		a prime record. The prime record's id is the globalid value for the
		correlated records regardless of record type.
		
		           prime, 
		             0
		     ________|_________
		    /        |         \
		  10         20         20
		 /           |           \
		Foo         Bar          Baz
		*/		
		select * from MembershipChanges as mc
		join stargate as sg
		on sg.globalid = mc.globalid
		and sg."partition" = `$2
	)


	select lg.localId as groupid, lmm.localid, lmm.upn, lmm."partition", lmm."action" from LocalMappedMembers as lmm
	join SelectedLocalGroup as lg
	on lg.partition = lmm.partition
`$$
LANGUAGE 'sql'
;
"@ 

    Start-Sleep -Seconds 15 -Verbose

    Import-Module SimplySQL

    $user = 'pguser'
    $pass = ConvertTo-SecureString "egm7DfeK" -AsPlainText -Force
    $cred = New-Object pscredential $user, $pass

    # TODO refactor this out into an environment variable
    $dbServer = "localhost"

    try {
        do {
            Open-PostGreConnection `
                -ConnectionName 'idDbMerge' `
                -TrustSSL `
                -Server $dbServer `
                -Credential $cred `
                -ErrorAction Stop `
                -Verbose
        } while (-not @(Get-SqlConnection -ConnectionName 'idDbMerge'))

        Write-Host "Creating identity database."
        Start-Sleep -Seconds 2
        Invoke-SqlUpdate -ConnectionName 'idDbMerge' `
            -Query $sqlQueryCreateDB -ErrorAction Stop
    
    }
    finally {
        Close-SqlConnection -ConnectionName 'idDbMerge'
    }
}
end {
    Pop-Location
}

