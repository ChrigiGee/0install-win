[CustomMessages]
dotnetfx40_title=.NET Framework 4.0
dotnetfx40_size=10 - 50 MB


[Code]	
const
	dotnetfx40_url = 'http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe';

procedure dotnetfx35or40();
var
	install: cardinal;
	installClient: cardinal;
begin
	// Check for .NET 3.5 but install .NET 4.0
	RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v3.5', 'Install', install);
	if (install <> 1) then
		AddProduct('dotNetFx40_Full_setup.exe',
			'/qb /norestart',
			CustomMessage('dotnetfx40_title'),
			CustomMessage('dotnetfx40_size'),
            dotnetfx40_url);
end;
