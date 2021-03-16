function Get-WordDefinition {
	param(
		[Parameter(Mandatory=$true)][string[]]$Words,
		[Parameter()][string]$LanguageCode
	)

	if("" -eq $LanguageCode) {
		$LanguageCode = "en_GB";
	}

	foreach($word in $Words){
		$definitions = Invoke-RESTMethod "https://api.dictionaryapi.dev/api/v2/entries/$LanguageCode/$word"
		$definition = $definitions[0]

		$x = [PSCustomObject]@{
			Word = $definition.word
			Phonetics = $definition.phonetics.text
		}

		foreach	($meaning in $definition.meanings) {
			$name = $meaning.partOfSpeech
			$first = $name.Substring(0,1);
			$name = $first.toupper() + $name.substring(1)

			Add-Member -InputObject $x -Name $name -Value $meaning.definitions[0].definition -MemberType NoteProperty
		}

		Write-Output $x
	}
}

Set-Alias Get-Definition Get-WordDefinition
Set-Alias Define-Word Get-WordDefinition
Export-ModuleMember -function Get-WordDefinition -alias Get-Definition,Define-Word