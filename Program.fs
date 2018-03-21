open System
open System.Collections.Generic;
open System.IO;
open System.Linq;
open System.Text;
open System.Threading;
open System.Threading.Tasks;
open Google.Apis.Auth.OAuth2;
open Google.Apis.Drive.v2;
open Google.Apis.Drive.v2.Data;
open Google.Apis.Services;
open Google.Apis.Util.Store;

[<EntryPoint>]
let main argv =

    let logPath = "log.txt"
    let logAction(fileId: string, emailAddress: string) =
        
        let time = DateTime.Now.ToLongDateString()
        File.AppendAllText(logPath, time + " " + fileId + " " + emailAddress)        

    let whitelist = File.ReadAllLines("whitelist.txt") //load whitelist

    let Scopes = [DriveService.Scope.Drive]
    let ApplicationName = "CompleteTeamDriveMigration";

    let path = "client_secret.json"
    let stream = new FileStream(path, FileMode.Open)
    let baseDirectory = Directory.GetParent(__SOURCE_DIRECTORY__).FullName
    let credPath = Path.Combine(baseDirectory, ".credentials/drive-dotnet-quickstart.json")
    
    let credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, Scopes, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result;
    let service = new DriveService(new BaseClientService.Initializer(HttpClientInitializer = credential,  ApplicationName = ApplicationName))
    
    let teamDriveId = "0AA9HWWw9ypOlUk9PVA"
    let listRequest = service.Files.List(Corpora = "teamDrive", SupportsTeamDrives = System.Nullable<bool>(true), TeamDriveId = teamDriveId, IncludeTeamDriveItems = System.Nullable<bool>(true))
    listRequest.MaxResults = new System.Nullable<int>(10) |> ignore
    
    let files = listRequest.Execute().Items

    let removeUser(fileId : string) =
        
        let permissions = service.Permissions.List(fileId, SupportsTeamDrives = System.Nullable<bool>(true)).Execute().Items
        for perm in permissions do
            let permissionId = perm.Id
            if perm.TeamDrivePermissionDetails.Item(0).TeamDrivePermissionType = "file" then
                if perm.WithLink <> System.Nullable<bool>(true) then
                    if not <| Array.contains perm.EmailAddress whitelist then
                        logAction(fileId, perm.EmailAddress)
                        service.Permissions.Delete(fileId, permissionId, SupportsTeamDrives = System.Nullable<bool>(true)).Execute() |> ignore
        0

    for file in files do
        removeUser(file.Id) |> ignore
    
    0