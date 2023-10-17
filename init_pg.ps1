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

