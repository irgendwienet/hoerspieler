#r "nuget: SpotifyAPI.Web"
#r "nuget: System.Net.Http"
#r "nuget: QRCoder"

open System.IO
open System.Net
open System.Text
open System.Text.RegularExpressions
open SpotifyAPI.Web
open QRCoder

let rootDir = "c:\output"

// get the token here https://developer.spotify.com/console/get-playlist/?playlist_id=&market=&fields=&additional_types=
let spotify = new SpotifyClient("xxx")

// Artist 

let artist = "0jRC2uXx4hEpqNrBDJdY7l"

spotify.Artists.Get(artist).Result
let page1 = spotify.Artists.GetAlbums(artist).Result

let all = spotify.PaginateAll(page1).Result 
            //|> Seq.filter (fun a -> a.Name.Contains "Folge") 
            |> Seq.sortBy (fun a -> a.ReleaseDate) 
            |> Seq.toList

let nummerRegex = Regex("Folge (\d+): (.*)")
let webClient = new WebClient()
let qrGenerator = new QRCodeGenerator()

let sb = new StringBuilder()
let mutable i = 1
for album in all do
    let episode = i.ToString()
    let title = nummerRegex.Match(album.Name).Groups.Item(2).Value    
    let artist = (album.Artists.Item(0).Name)
    let image = album.Images |> Seq.sortByDescending (fun i -> i.Width) |> Seq.head
    let link = sprintf "spotify:album:%s" album.Id

    webClient.DownloadFile(image.Url, (sprintf "%s\\img\\cover_%s.jpg" rootDir episode))
    
    let qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q)
    let qrCode = new PngByteQRCode(qrCodeData);
    let qrCodeAsPngByteArr = qrCode.GetGraphic(20);
    File.WriteAllBytes( (sprintf "%s\\img\\qr_%s.png" rootDir episode), qrCodeAsPngByteArr)

    sb
        .AppendLine("\\begin{tikzpicture}")
        .AppendLine(sprintf "    \cardbackground{img/cover_%s.jpg}" episode)
        //.AppendLine(sprintf "    \cardtitle{%s}" artist)
        //.AppendLine(sprintf "    %% %s" title)
        .AppendLine(sprintf "    \cardcontent{} { \includegraphics[width=0.65\\textwidth]{img/qr_%s.png} }" episode)
        //.AppendLine(sprintf "    \cardprice{%s}" episode)
        .AppendLine(sprintf "    \cardborder")
        .AppendLine("\end{tikzpicture}")
        //.AppendLine("\hspace{5mm}")
        |> ignore

    if i%5 = 0 then
        sb.AppendLine("\\vspace{5mm}") |> ignore

    i <- i+1

File.WriteAllText((sprintf "%s\\cards.tex" rootDir), sb.ToString())
