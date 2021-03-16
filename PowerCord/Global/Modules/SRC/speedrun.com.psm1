$APIURL = "https://www.speedrun.com/api/v1"
$VARIABLECACHE = @{}

Function Invoke-SRCMethod {
	param($Url)

	# Write-Host "$APIURL/$Url"	
	return (Invoke-RestMethod "$APIURL/$Url").data
}

function Get-Game($name) {
	if ($name -match '\s') {
		$games = Invoke-SRCMethod -Url "games?name=$name"
		if ($null -eq $games -or $games.length -eq 0) {
			Write-Error "Game not found!"
			return $null
		}

		return $games[0]
	}
	else {
		return Invoke-SRCMethod -Url "games/$name"
	}
}

function Get-GameCategories($game) {
	return Invoke-SRCMethod "games/$($game.id)/categories"
}

function Get-PlayerUsername($players, $player) {
	if ($player.rel -eq "user") {
		$user = @($players | Where-Object { $_.id -eq $player.id })[0]
		return $user.names.international
	}

	return $player.name
}

function Get-Variable($id) {
	if ($null -eq $VARIABLECACHE[$id]) {
		$VARIABLECACHE[$id] = Invoke-SRCMethod "variables/$($variable.name)"
	}
	
	return $VARIABLECACHE[$id]
}

function Get-Leaderboard {
	param(
		[Parameter(ParameterSetName = "ById", Position = 0)][string]$GameId,
		[Parameter(ParameterSetName = "ById", Position = 1)][string]$CategoryId,
		[Parameter(ParameterSetName = "ByCategory", Position = 0)][PSCustomObject]$Category,
		[int]$Limit = 20,
		[nullable[Datetime]]$Date,
		[HashTable]$Variables
	)

	$game = ""
	$gameName = ""
	$categoryName = ""

	if ($null -ne $Category) {
		$game = Get-Game $Category.GameId
		if ($null -eq $game) {
			return
		}

		$gameName = $game.names.international		
		$CategoryId = $Category.Id 
		$categoryName = $Category.Name
	}
	else {
		$game = Get-Game $gameId
		if ($null -eq $game) {
			return
		}

		$gameName = $game.names.international
		$categories = Get-GameCategories $game
		if ("" -eq $CategoryId) {
			foreach	($cat in $categories) {
				if ($cat.type -eq "per-game") {
					$CategoryId = $cat.id 
					$categoryName = $cat.name
					break;
				}
			}
		}
		else {
			foreach	($cat in $categories) {
				if ($cat.name -like $CategoryId -and $cat.type -eq "per-game") {
					$CategoryId = $cat.id 
					$categoryName = $cat.name
					break;
				}
			}
		}
	}


	$leaderboardUrl = "leaderboards/$($game.id)/category/${CategoryId}?embed=players,variables&top=$Limit"
	if ($null -ne $Date) {
		$leaderboardUrl = "$leaderboardUrl&date=$($Date.ToString("o"))" 
	}
	
	if ($null -ne $Variables) {
		$vars = Invoke-SRCMethod "categories/$CategoryId/variables"
		foreach ($key in $Variables.Keys) {
			$val = $Variables[$key]
			$var = @($vars | Where-Object { ($_.id -eq $key) -or ($_.name -like $key) } | Select-Object -First 1)
			if ($null -eq $var) {
				continue
			}

			$choice = @($var.values.choices.PSObject.Properties | Where-Object { ($_.name -eq $val) -or ($_.value -like $val) } | Select-Object -First 1)
			$leaderboardUrl = "$leaderboardUrl&var-$($var.id)=$($choice.name)" 
		}
	}

	$leaderboard = Invoke-SRCMethod $leaderboardUrl
	$players = $leaderboard.players.data
	$lbVariables = $leaderboard.variables.data

	foreach ($runContainer in $leaderboard.runs) {
		$run = $runContainer.run
		$time = [TimeSpan]::fromseconds([double]$run.times.primary_t)
		$date = [DateTime]::parse($run.date)

		$returnObject = [PSCustomObject]@{
			Game        = $gameName
			Category    = $categoryName
			Time        = $time
			Date        = $date
			Runners     = @($run.players | ForEach-Object { Get-PlayerUsername $players $_ })
			VerifiedAt  = $run.status."verify-date"
			Description = $run.comment
		}
		
		if ($null -ne $run.videos.links) {
			$videoUrl = $run.videos.links[0]
			if ($null -ne $videoUrl) {
				Add-Member -InputObject $returnObject -Name "VideoUrl" -Value $run.videos.links[0].uri -MemberType NoteProperty
			}
		}

		foreach ($variable in $run.values.PSObject.Properties) {
			$variableObj = @($lbVariables | Where-Object { $_.id -eq $variable.name })[0]
			$label = $variableObj.values.choices | select-object -ExpandProperty $variable.value 

			Add-Member -InputObject $returnObject -Name $variableObj.name -Value $label -MemberType NoteProperty
		}

		Write-Output $returnObject
	}
}

function Get-WorldRecord {
	param(
		[Parameter(ParameterSetName = "ById", Position = 0)][string]$GameId,
		[Parameter(ParameterSetName = "ById", Position = 1)][string]$CategoryId,
		[Parameter(ParameterSetName = "ByCategory", Position = 0)][PSCustomObject]$Category,
		[HashTable]$Variables
	)

	if ($null -eq $Category) {
		Get-Leaderboard -GameId $GameId -CategoryId $CategoryId -Variables $Variables -Limit 1 | Write-Output
	}
	else {
		Get-Leaderboard -Category $Category -Variables $Variables -Limit 1 | Write-Output
	}
}

function Get-Category {
	param (
		[Parameter(Mandatory)][string]$GameId,
		[string]$Name
	)

	$game = Get-Game $gameId
	if ($null -eq $game) {
		return
	}

	$categories = Get-GameCategories $game
	foreach	($category in $categories) {
		if ("" -ne $Name -and $category.name -notlike $Name) {
			continue
		}

		write-output $category
	}
}

Set-Alias -Name Get-WR -Value Get-WorldRecord

Export-ModuleMember -function Get-Game
Export-ModuleMember -function Get-Category
Export-ModuleMember -Function Get-Leaderboard
Export-ModuleMember -Function Get-WorldRecord -Alias Get-WR
