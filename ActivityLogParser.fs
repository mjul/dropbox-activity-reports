module ActivityLogParser

open System
open System.IO
open FSharp.Data


[<Literal>]
let  ActivityLogTemplateFile = __SOURCE_DIRECTORY__ + "/" + "activity-log-template.csv"

type ActivityLogCsv = CsvProvider<ActivityLogTemplateFile,IgnoreErrors=false>
type FileDescriptorJson = JsonProvider<"""{"path": "/Articles/Interesting article.pdf", "is_dir": false, "file_id": "aGTP3TBWMKARRRAAADAALF", "host_id": 1234567890}""">

type Activity =
    | FileAdded of path : string 
    | FileEdited of path : string
type User = | User of name : string * email : string
type Event = {Time: DateTime; Activity: Activity; User: User}
    

let parseFileRow (row:ActivityLogCsv.Row) =
    let parseInfo () =
        FileDescriptorJson.Parse(row.Info)
    match row.``Event type`` with
    | "Edited files" ->
        Some (FileEdited (parseInfo().Path))
    | "Added files" ->
        Some (FileAdded (parseInfo()).Path)
    | _ ->
        None
        
let parseRow (row:ActivityLogCsv.Row) =
    let user = User (row.Name, row.Email)
    let activity =
        match row.``Event category`` with
        | "Files" ->
            parseFileRow row
        | _ ->
            None
    match activity with
    | Some a ->
        Some {Time=row.Time; Activity=a; User=user}
    | None ->
        None

(* Parse the log and return the rows that we can parse *)
let parseRows rows =
    rows
    |> Seq.map parseRow
    |> Seq.choose id


(*
   Dropbox sometimes changes the format so it is good to check that
   the parser and the the file match.
*)
let validateFileFormat fname =
    // We can get the expected headers in the file by saving a file with no
    // records to a string. Then, we can compare this to the actual header.
    use emptyCsv = new ActivityLogCsv([])
    let expectedHeader = emptyCsv.SaveToString().Trim()
    // note this fails if the file is empty 
    let actualHeader = (File.ReadLines(fname) |> Seq.head).Trim()
    if actualHeader = expectedHeader then
        None
    else
        Some (sprintf "Invalid header: expected %s, got %s." expectedHeader actualHeader)

let validateFile fname =
    if System.IO.File.Exists(fname) then
        validateFileFormat fname
    else
        Some (sprintf "File not found: %s" fname)

let parseActivityLog (fname:string) =
    match (validateFile fname) with
    | Some error ->
        Some (sprintf "Error while parsing file %s: %s" fname error), Seq.empty
    | None ->
        let rows = ActivityLogCsv.Load(fname).Rows
        None, parseRows rows
