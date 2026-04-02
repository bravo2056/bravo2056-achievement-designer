@echo off
cd /d C:\stream
git add achievements-data.json
git commit -m "achievement update"
git push
echo.
echo Done! Achievement Hall will update within 2 minutes.
pause
