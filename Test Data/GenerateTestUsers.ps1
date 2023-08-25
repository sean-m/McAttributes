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


#region TimeEstimator
 <#
A time estimator for use with PowerShell's Write-Progress. Provided a total
number of cycles, it tracks how long the last 100 or less itterations of your
main loop took and calculates the remaining time based on velocity. Note: when
ticks exceed total, it returns a seconds remaining of 0, but continues to track
the rate that work is getting done.
The intended use case is with Write-Progress, calls to that cmdlet are really
slow on PowerShell 2-5, efforts have been made to maintain low overhead. If
you're after performance this is still useful, just log the progress yourself.
For instance, using [System.Diagnostics.Trace]::Write() and watching with
Sysinternals DbgView is a faster alternative than logging to the console. How
much sense that makes when worrying about wallclock time is up to you.
#>
class TimeEstimator {

    $Total

    hidden [long] $Ticks = 0
    static hidden $bufferSize = 128 # keep this as power of 2
    hidden $Tickline = (New-Object long[] ([TimeEstimator]::bufferSize))
    hidden $Timeline = (New-Object long[] ([TimeEstimator]::bufferSize))
    hidden [double] $Rate = 0.0
    hidden $ComputeInterval
    hidden $LastComputeState = @([DateTime]::Now.Ticks,0)
    hidden $DynamicInterval = $false

    hidden [int] ParseTotal ($Total) {
        if ($Total -is [array]) {
            return $Total.Count
        } elseif ($Total -match '^[\d\.]+') {
            return [int]$Total
        }
        #TODO can't initialize with a numeric value for total so can't compute
        # percentage complete, but could still measure the processing rate.
        # Will probably just crash things but using this wrong should never crash
        # a script, should just show a warning.

        return $Total
    }

    # Initialize with a total amount of itterations you're expecting but let the
    # script figure out how frequently to compute the rate of work. Currently
    # it tries to balance out by recomputing every 11 seconds or so. If your
    # work happens faster than that, you probably don't need a progress bar.
    TimeEstimator($Total) {
        $this.Total = $this.ParseTotal($Total)
        $this.ComputeInterval = [int]([Math]::Max(1,[Math]::Min(5, $this.Total / 10)))
        $this.DynamicInterval = $true
        $this.LastComputeState = @([datetime]::Now.Ticks, $this.Ticks)
    }

    # Initialize with a total amount of itterations you're expecting and how frequently you
    # want to compute the rate of work. Compute interval is in iterations not wall clock time.
    TimeEstimator([int]$Total, [int]$ComputeInterval) {
        $this.Total = $this.ParseTotal($Total)
        $this.ComputeInterval = [Math]::Max($ComputeInterval, 16) # Don't compute more frequently than every 15. For slow stuff, should probably let the script figure it out by not supplying a ComputeInterval to the constructor.
        $this.LastComputeState = @([datetime]::Now.Ticks, $this.Ticks)
    }

    [void] Tick() {
        $this.Ticks++
        $i = $this.Ticks -band ([TimeEstimator]::bufferSize - 1)
        $this.Tickline[$i] = $this.Ticks
        $this.Timeline[$i] = [datetime]::Now.Ticks

        if ($this.Ticks % $this.ComputeInterval -eq 0 `
        -or ([DateTime]::Now.Ticks - $this.LastComputeState[0]) / [timespan]::TicksPerSecond -gt 11) {
            # Generate a time estimate based on work done recently

            # TODO improve by doing a rolling weighted average based on how many have been performed minute over minute since the beginning,
            # ignoring minutes which fall outside the stddev for the last 2 minutes. Ideally it should compute a linear regression for
            # estimating but that would require some kind of sampling scheme to keep data use bounded. Perhaps a second ring buffer that
            # collects samples based on wallclock time, no more than every 20 seconds or so to perform linear regression on. Needs benchmarking
            # to determine performance impact but could easily use System.Numerics.Vector<T> to make use of SIMD acceleration for multiplication.
            
            $measurement = ($this.Timeline.Where({ $_ -ne 0 }) | Measure-Object -Minimum -Maximum)
            $span = [double]([decimal]($measurement.Maximum - $measurement.Minimum) / [timespan]::TicksPerSecond)
            $this.Rate = ([double]($measurement.Count) / $span)
            #Write-Host ("Measurement count: {0} timespan: {1}  tick min {2}  tick max {3}" -f $measurement.Count, $span, $measurement.Minimum, $measurement.Maximum)
            <#
            $span = ([decimal]([datetime]::Now.Ticks - $this.LastComputeState[0]) / [timespan]::TicksPerSecond)
            if ($span -eq 0) { return }
            $this.Rate = ([double]($this.Ticks - $this.LastComputeState[1]) / $span)
            #>

            # When computing the interval dynamically, target resampling every 10 seconds or
            # doing at least 10 samples for the given input set if processing things quicly.
            # Yes this adds overhead to very quick operations but if you're doing something
            # super fast, you shouldn't be using Write-Progress.
            if ($this.DynamicInterval) {
                $this.ComputeInterval = [int]([Math]::Max(2.0, [Math]::Min(10.0 * $this.Rate, $this.Total / 10.0)))
            }
            $this.LastComputeState = @([datetime]::Now.Ticks, $this.Ticks) # capture time and number of itterations
        }
    }

    [string] GetStatusQuote() {
        if ($this.Rate -eq 0.0) { return 'Too early to tell' }
        return "More than {0}/{1} completed. Rate: {2:N2} / sec" -f $this.Ticks, $this.Total, $this.Rate
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

    [void] ShowDefaultProgress () {
        $this.ShowDefaultProgress("Processing pipeline")
    }

    [void] ShowDefaultProgress ($Msg) {
        Write-Progress -Activity $Msg `
            -Status ($this.GetStatusQuote()) `
            -PercentComplete ($this.GetPercentage()) `
            -SecondsRemaining ($this.GetSecondsRemaining())
    }
}
filter Write-PipelineProgress {
    param ([TimeEstimator]$Estimator)
    if ($Estimator) {
        $Estimator.Tick()
        $Estimator.ShowDefaultProgress()
    }
    $_
}
#endregion TimeEstimator


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
$spoofRate   = 0.04
$guestRate   = 0.8

################################################################################
##                                    Main                                    ##
################################################################################
$recordCount = 1000 * 10

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

    $fetched = (Get-Date).AddHours(- (Get-Random -Minimum 0 -Maximum 23 )).AddSeconds(- (Get-Random -Minimum 0 -Maximum 3600))
    
    $account = New-Object PSObject -Property @{
        merged = $fetched.AddSeconds((Get-Random -Min 300 -Max 6555))
        modified = (Get-Date).AddDays(- (Get-Random -Minimum 0 -Maximum 365)).AddHours(- (Get-Random -Minimum 0 -Maximum 23 )).AddSeconds(- (Get-Random -Minimum 0 -Maximum 3600))
        lastFetched = $fetched
        created = (Get-Date).Addyears(- (Get-Random -Minimum 0 -Maximum 4)).AddDays(- (Get-Random -Minimum 0 -Maximum 365)).AddHours(- (Get-Random -Minimum 0 -Maximum 23 )).AddSeconds(- (Get-Random -Minimum 0 -Maximum 3600))
        enabled = if ((RndFloat) -le 0.005) { $false } else { $true }
        tenant = $tenant
        aadId = [Guid]::NewGuid()
        upn = "$first`.$last@$tenant"
        mail = "$first`.$last@$($tenant.Replace('.onthecloud',''))"
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

    $account

    if (RndFloat -le $guestRate) {
        $tenant | where $account.Upn -NotLike "*$_" | foreach {
            $guest = $account.PSObject.Copy()
            $upn = $guest.upn.Replace("@","_") + "#EXT#@" + $_
            $guest.upn = $upn
            $guest.creationType = 'Invitation'
            $guest.aadId = [Guid]::NewGuid()
            $guest
        }
    }
} | export-csv -NoTypeInformation .\test_azusers_1.csv