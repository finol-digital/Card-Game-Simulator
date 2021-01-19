# Activate License
Start-Process -FilePath $env:UnityPath -ArgumentList '-batchmode','-quit','-logfile','activatelicense.log','-nographics','-username',$env:UNITY_EMAIL,'-password',$env:UNITY_PASSWORD,'-serial',$env:UNITY_SERIAL -NoNewWindow -Wait
Get-Content activatelicense.log

# Build Standalone
Start-Process -FilePath $env:UnityPath -ArgumentList '-batchmode','-quit','-logfile','build-windows.log','-executeMethod','Cgs.Editor.BuildCgs.BuildWindows' -NoNewWindow -Wait
Get-Content build-windows.log

# Build Standalone64
Start-Process -FilePath $env:UnityPath -ArgumentList '-batchmode','-quit','-logfile','build-windows64.log','-executeMethod','Cgs.Editor.BuildCgs.BuildWindows64' -NoNewWindow -Wait
Get-Content build-windows64.log

# Build Uwp
Start-Process -FilePath $env:UnityPath -ArgumentList '-batchmode','-quit','-logfile','build-uwp.log','-executeMethod','Cgs.Editor.BuildCgs.BuildUwp' -NoNewWindow -Wait
Get-Content build-uwp.log

# Return License
Start-Process -FilePath $env:UnityPath -ArgumentList '-batchmode','-quit','-logfile','returnlicense.log','-returnlicense' -NoNewWindow -Wait
Get-Content returnlicense.log