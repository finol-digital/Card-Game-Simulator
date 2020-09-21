      - name: Activate License
        env:
          Args: -batchmode –quit -logfile activate.stdout.txt -nographics -username ${{ secrets.UNITY_EMAIL }} -password ${{ secrets.UNITY_PASSWORD }} -serial ${{ secrets.UNITY_SERIAL }}
        run: Start-Process -FilePath 'C:\Program Files\Unity\Editor\Unity.exe' -ArgumentList $env:Args -NoNewWindow -Wait
        env:
          Args: -batchmode –quit -logfile build-windows.stdout.txt -executeMethod Cgs.Editor.BuildCgs.BuildWindows
        run: Start-Process -FilePath 'C:\Program Files\Unity\Editor\Unity.exe' -ArgumentList $env:Args -NoNewWindow -Wait
      - name: Build StandaloneWindows64
        env:
          Args: -batchmode –quit -logfile build-windows64.stdout.txt -executeMethod Cgs.Editor.BuildCgs.BuildWindows64
        run: Start-Process -FilePath 'C:\Program Files\Unity\Editor\Unity.exe' -ArgumentList $env:Args -NoNewWindow -Wait
      - name: Build Uwp
        env:
          Args: -batchmode –quit -logfile build-uwp.stdout.txt -executeMethod Cgs.Editor.BuildCgs.BuildUwp
        run: Start-Process -FilePath 'C:\Program Files\Unity\Editor\Unity.exe' -ArgumentList $env:Args -NoNewWindow -Wait
      - name: Return License
        env:
          Args: -batchmode –quit -logfile returnlicense.stdout.txt -returnlicense
        run: Start-Process -FilePath 'C:\Program Files\Unity\Editor\Unity.exe' -ArgumentList $env:Args -NoNewWindow -Wait

Get-Content activate.stdout.txt
Get-Content build-windows.stdout.txt
Get-Content build-windows64.stdout.txt
Get-Content build-uwp.stdout.txt
Get-Content returnlicense.stdout.txt