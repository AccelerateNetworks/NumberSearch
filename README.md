# NumberSearch
 Lookup available phone numbers for purchase.
 
 [Try it out here!](https://numbersearch.acceleratenetworks.com/)

# Run locally on Windows 10
* Install [Visual Studio 2019 Community Edition](https://visualstudio.microsoft.com/vs/).
* Open Visual Studio 2019 (VS2019).
* Select the "Clone or check out code" option under "Getting Started" on the project selection splash screen.
* Login to Github if required.
* Clone this repo from the Master branch.
* With the project directory now open in VS2019 click the "NumberSearch.sln" file to configure Visual Studio.
* Click on the tab labelled "Package Manager Console" at the bottom of VS 2019.
* Run this command to setup a local secrets store "dotnet user-secrets init --project .\NumberSearch.Mvc".
* Then run this command "dotnet user-secrets init --project .\NumberSearch.Tests".
* More info and troubleshooting details on configuring [user-secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.0&tabs=windows).
* Run this command to register a secret "dotnet user-secrets set "ConnectionStrings:TeleAPI" "<!replaceThisWithYourAPIKey>" --project .\NumberSearch.Mvc"
* Then run this command "dotnet user-secrets set "ConnectionStrings:TeleAPI" "<!replaceThisWithYourAPIKey>" --project .\NumberSearch.Tests"
* At the top center of the VS2019 window there is a button with a green arrow labelled "IIS Express" this will run the project on a local web server.
* Click the dropdown arrow next to it to and hover over the "Web Browswer" option to set ISS Express to lanuch the application with your perfered web browser (Chrome) by default.
* Click the green arrow to run the project on localhost.
