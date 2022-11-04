#Requires -Version 5

################################################################################
##                                  Functions                                 ##
################################################################################

filter Percentage { 
    param ([ValidateRange(0,[int32]::MaxValue)]$count=0, 
           [ValidateRange(1,[int32]::MaxValue)]$total=1) 
    [Math]::Min(100, [Math]::Round(($count/$total) * 100))
}

function RndFloat { [float](Get-Random -Minimum 0 -Maximum 10000) / 10000 }

class TimeEstimator {

    $Total

    hidden $Ticks = 0
    static hidden $bufferSize = 100
    hidden $Timeline = (New-Object decimal[] ([TimeEstimator]::bufferSize))
    hidden [double] $Rate = 0.0
    hidden $ComputeInterval

    TimeEstimator($Total) {
        $this.Total = $Total
        $this.ComputeInterval = [int]([Math]::Min(100, $Total / 10))
    }

    TimeEstimator([int]$Total, [int]$ComputeInterval) {
        $this.Total = $Total
        $this.ComputeInterval = [Math]::Max($ComputeInterval, 15)
    }
    
    [void] Tick() {
        $this.Ticks++
        $this.Timeline[$this.Ticks % [TimeEstimator]::bufferSize] = [DateTime]::Now.Ticks
        
        if ($this.Ticks % $this.ComputeInterval -eq 0) {
            # Generate a time estimate based on work done in the last 30 seconds
            # TODO improve by doing a rolling weighted average based on how many have been performed minute over minute since the beginning, ignoring minutes which fall outside the stddev for the last 2 minutes
            $measurement = ($this.Timeline.Where({ $_ -ne 0 }) | measure -Minimum -Maximum)
            $done_this_minute = $measurement.Count
            $this.Rate = ([double]($measurement.Count) / ([decimal]($measurement.Maximum - $measurement.Minimum) / [timespan]::TicksPerSecond))
            
        }
    }

    [string] GetStatusQuote() {
        if ($this.Rate -eq 0.0) { return 'Too early to tell.' }
        return "More than {0}/{1} completed. Rate: {2:N2} / sec." -f $this.Ticks, $this.Total, $this.Rate
    }

    [int] GetSecondsRemaining() {
        if ($this.Rate -eq 0.0) { return [int]::MaxValue }
        return  [Math]::Max(0, [Math]::Min([int]::MaxValue, (($this.Total - $this.Ticks) / $this.Rate)))
    }

    [int] GetPercentage () {
        filter Percentage { 
            param ([ValidateRange(0,[int32]::MaxValue)]$count=0, 
                   [ValidateRange(1,[int32]::MaxValue)]$total=1) 
            [Math]::Min(100, [Math]::Round(($count/$total) * 100))
        }

        return (Percentage -count ($this.Ticks) -total ($this.Total))
    }
}


################################################################################
##                                Configuration                               ##
################################################################################

cd $(Split-Path -Parent -Path $MyInvocation.MyCommand.Definition)

$tenants = @"
Jaws of Death
Down To The Wire
It's Not All It's Cracked Up To Be
A Busy Body
A Few Sandwiches Short of a Picnic
Down For The Count
A Cut Above
Back To the Drawing Board
A Lemon
Hear, Hear
A Dime a Dozen
"@ -split "`n" | foreach {
    $phrase = $_
    $alpha = ($phrase.ToCharArray() | where { $_ -match '[a-zA-Z]' }) -join ''
    "$alpha.onthecloud.com"
}

$firstNames = gc .\firstNames.txt | where { $_ } | foreach { $_.Trim() }
$lastNames = gc .\lastNames.txt | where { $_ } | foreach { $_.Trim() }

$articles = @(
"Dr",
"MD",
"Sir",
"Dame",
"Esq"
)

$pronouns = @(
"Ey/Em/Eir",
"He/Him/His",
"Ne/Nem/Nir",
"Per/Per/Per",
"She/Her/Hers",
"They/They/Them",
"Ve/Ver/Vis",
"Xe/Xem/Xyr",
"Zie/Zim/Zir"
)


$employeeIds = @()

$pronounRate = 0.01
$articleRate = 0.001
$spoofRate = 0.0001





################################################################################
##                                    Main                                    ##
################################################################################
$recordCount = 1000 * 50

$estimator = [TimeEstimator]::new($recordCount, 100)
1..$recordCount | foreach {
    
    $estimator.Tick()
    Write-Progress -Activity "Stirring this mess up.." `
        -Status ($estimator.GetStatusQuote()) `
        -PercentComplete ($estimator.GetPercentage()) `
        -SecondsRemaining ($estimator.GetSecondsRemaining())
    
    $article = ''
    $x = RndFloat
    if ($articleRate -ge $x) {
        $article = Get-Random -InputObject $articles
    }

    $pronoun = ''
    $x = RndFloat
    if ($pronounRate -ge $x) {
        $pronoun = Get-Random -InputObject $pronouns
    }

    $first = Get-Random -InputObject $firstNames

    $last = Get-Random -InputObject $lastNames

    $tenant = Get-Random -InputObject $tenants

    $employeeId = "OT{0:D7}" -f (Get-Random -min 999 -max 999999)
    $employeeIds += $employeeId

    New-Object PSObject -Property @{
        merged = $false
        modified = (Get-Date).AddDays(- (Get-Random -Minimum 0 -Maximum 365)).AddHours(- (Get-Random -Minimum 0 -Maximum 23 )).AddSeconds(- (Get-Random -Minimum 0 -Maximum 3600))
        lastFetched = (Get-Date).AddHours(- (Get-Random -Minimum 0 -Maximum 23 )).AddSeconds(- (Get-Random -Minimum 0 -Maximum 3600))
        created = (Get-Date).Addyears(- (Get-Random -Minimum 0 -Maximum 4)).AddDays(- (Get-Random -Minimum 0 -Maximum 365)).AddHours(- (Get-Random -Minimum 0 -Maximum 23 )).AddSeconds(- (Get-Random -Minimum 0 -Maximum 3600))
        enabled = if ((RndFloat) -le 0.005) { $false } else { $true }
        tenant = $tenant
        aadId = [Guid]::NewGuid()
        upn = "$first`.$last@$tenant"
        mail = "$first`.$last@$tenant"
        employeeId = $employeeId
        adEmployeeId = if (RndFloat -le $spoofRate) { $i = Get-Random -Minimum 0 -Maximum $employeeIds.Count; $employeeIds[$i] } else { $employeeId }  # randomly select from previous employeeIds that have been issued consistent with the spoofing rate
        hrEmployeeId = ''
        wid = ''
        creationType = ''
        company = '' 
        displayName = ''
        preferredGivenName = $first
        preferredSurname = $last
        articles = $article
        pronouns = $pronoun
    }
} | export-csv -NoTypeInformation .\test_azusers_1.csv