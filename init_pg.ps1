<#
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
	id serial primary key,
	created timestamp,
	dataSourceId TEXT,
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
	logType TEXT,
	message TEXT,
	file TEXT,
	jobId TEXT
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
 id SERIAL PRIMARY KEY,
 merged TIMESTAMP,
 modified TIMESTAMP,
 lastFetched TIMESTAMP,
 created TIMESTAMP,
 enabled BOOLEAN,
 tenant TEXT,
 aadId UUID UNIQUE,
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
 pronouns TEXT
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
	id SERIAL PRIMARY KEY,
	partition VARCHAR(64) NOT NULL,
	azgroupId VARCHAR(40) NOT NULL,
	azgroupName VARCHAR(256) NOT NULL,
	memberId INTEGER NOT NULL,
	memberType VARCHAR(12),
	UNIQUE (azgroupId, memberId)
);

CREATE INDEX IF NOT EXISTS idx_azgrpmember_groupId ON azgroupmembers (azgroupid);
CREATE INDEX IF NOT EXISTS idx_azgrpmember_groupName ON azgroupmembers (azgroupName);
CREATE INDEX IF NOT EXISTS idx_azgrpmember_groupmember ON azgroupmembers (memberId);


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
	id SERIAL PRIMARY KEY,
	recordType INTEGER,
	globalId INTEGER,
	localId VARCHAR(256) NOT NULL UNIQUE,
	upn VARCHAR(256),
	partition VARCHAR(256),
	joinSeed VARCHAR(256),
	
	CONSTRAINT globalId_fk 
		FOREIGN KEY (globalId)
		REFERENCES stargate(id)
);
CREATE INDEX IF NOT EXISTS idx_sg_joinSeed ON stargate(joinSeed);
CREATE INDEX IF NOT EXISTS idx_sg_localId ON stargate(localId);
CREATE INDEX IF NOT EXISTS idx_sg_globalId ON stargate(globalId);
CREATE INDEX IF NOT EXISTS idx_sg_partitionId ON stargate(partition);
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

DROP FUNCTION IF EXISTS getAzureGroupMembersPartitionCrosswalk(GroupName VARCHAR(256), Partition VARCHAR(256));
CREATE OR REPLACE FUNCTION getAzureGroupMembersPartitionCrosswalk(GroupName VARCHAR(256), Partition VARCHAR(256)) RETURNS TABLE (azGroupId VARCHAR(40), mappedId varchar(40))
AS `$$
	SELECT azGroupId, partitionCrosswalk(memberId, `$2::TEXT)::varchar FROM azgroupmembers
`$$
LANGUAGE 'sql'
;


DROP FUNCTION IF EXISTS getTotalNativeGroupMemberGlobalIds(GroupName VARCHAR(256));
CREATE OR REPLACE FUNCTION getTotalNativeGroupMemberGlobalIds(GroupName VARCHAR(256)) RETURNS TABLE (globalId INTEGER)
AS `$$
	SELECT azGroup.memberId FROM azgroupmembers as azGroup
	JOIN stargate AS sg1
	ON azGroup.memberId = sg1.globalId
	AND azGroup."partition" = sg1."partition"
	AND azGroup.azgroupname = `$1
	AND sg1.recordtype = 1 -- Get the group members that are native to the partition they are a member in. These are the users we actually want to replicate.
`$$
LANGUAGE 'sql'
;

/*
Given a group name and partition that you care about, get the list of actions to perform on the group members.
Results are returned as globalIds so they must still be crosswalked. 
*/
DROP FUNCTION IF EXISTS getNeededGroupMemberChanges(GroupName VARCHAR(256), Parititon VARCHAR(256));
CREATE OR REPLACE FUNCTION getNeededGroupMemberChanges(GroupName VARCHAR(256), Partition VARCHAR(256)) RETURNS TABLE (groupId TEXT, memberId TEXT, partition VARCHAR(256), change VARCHAR(256))
AS `$$

WITH changes AS (
	-- Get the list of users to remove from a given group in a given partition
	SELECT memberId, `$2 AS partition, 'remove' AS change FROM azgroupmembers WHERE  azgroupname = `$1 AND partition = `$2
	EXCEPT
	SELECT globalId AS memberId, `$2 AS partition, 'remove' AS change FROM gettotalnativegroupmemberglobalids(`$1)
	UNION
	-- Get the list of users to add to a given group in a given partition
	SELECT globalId AS memberId, `$2 AS partition, 'add' AS change FROM gettotalnativegroupmemberglobalids(`$1)
	EXCEPT 
	SELECT memberId, `$2 AS partition, 'add' AS change FROM azgroupmembers WHERE  azgroupname = `$1 AND partition = `$2
)

SELECT (SELECT azgroupid FROM azgroupmembers WHERE azgroupname = `$1 AND partition = `$2 LIMIT 1) AS groupId, m.partitioncrosswalk AS memberId, m.partition, m.change  FROM (SELECT partitioncrosswalk(memberId, `$2), partition, change FROM changes) m

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
"@ 

Start-Sleep -Seconds 10 -Verbose

Import-Module SimplySQL

$user = 'pguser'
$pass = ConvertTo-SecureString "egm7DfeK" -AsPlainText -Force
$cred = New-Object pscredential $user, $pass

# TODO refactor this out into an environment variable
$dbServer = "localhost"

try {
    Open-PostGreConnection `
        -ConnectionName 'idDbMerge' `
        -TrustSSL `
        -Server $dbServer `
        -Credential $cred `
        -ErrorAction Stop `
        -Verbose

    Write-Host "Creating identity database."
    Invoke-SqlUpdate -ConnectionName 'idDbMerge' `
        -Query $sqlQueryCreateDB -ErrorAction Stop
}
finally {
    Close-SqlConnection -ConnectionName 'idDbMerge'
}

Start-Sleep -Seconds 1

try {
    Write-Host 'Connecting to identity database.'    
    Open-PostGreConnection `
        -ConnectionName 'idDbMerge' `
        -TrustSSL `
        -Database 'identity' `
        -Server $dbServer `
        -Credential $cred `
        -ErrorAction Stop `
        -Verbose

    Write-Host 'Creating tables and functions.'
    Invoke-SqlUpdate -ConnectionName 'idDbMerge' `
        -Query $sqlQuery -ErrorAction Stop
        
}
finally {
    Close-SqlConnection -ConnectionName 'idDbMerge'
}