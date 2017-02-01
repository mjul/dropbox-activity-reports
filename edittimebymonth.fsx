#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#load "ActivityLogParser.fs"

open ActivityLogParser
open System

let firstDayOfMonth (d:DateTime) =
    new DateTime(d.Year, d.Month, 1)

let editIntervals (edits:DateTime seq) =
    edits
    |> Seq.sort
    |> Seq.pairwise
    |> Seq.map (fun (t1, t2) -> t2.Subtract(t1))


let DEFAULT_MINUTES_OF_WORK_PER_EDIT = 5.0

let estimatedWorkPerEdit editTimestamps =
    // assuming that people save at least once every two hours
    // when they are working continuously on something
    let sameEditSession =
        editTimestamps
        |> editIntervals
        |> Seq.filter (fun duration -> duration < TimeSpan.FromHours(2.0))
    match (Seq.length sameEditSession) with
    | 0 
    | 1 ->
        TimeSpan.FromMinutes(DEFAULT_MINUTES_OF_WORK_PER_EDIT)
    | _ -> 
        let averageEditInterval =
            sameEditSession
            |> Seq.map (fun t -> t.TotalSeconds)
            |> Seq.average
        TimeSpan.FromSeconds(averageEditInterval)
    

let estimatedEditTime editTimestamps =
    let numEdits = editTimestamps |> Seq.length
    let estimatedSeconds = (estimatedWorkPerEdit editTimestamps).TotalSeconds * (float numEdits)
    TimeSpan.FromSeconds(estimatedSeconds)


let editsOnly events =
    seq {
        for e in events do
        match e.Activity with
        | FileEdited path -> yield (e, path)
        | _ -> () }
    
let estimatedEditTimeByFileByMonth (events:Event seq) : ((DateTime * string * TimeSpan) seq) =
    let pathEditPairs = editsOnly events |> Seq.map (fun (e, path) -> (path, e.Time))
    let pathTimestampsPairs =
        pathEditPairs
        |> Seq.groupBy fst
        |> Seq.map (fun (path, pts) -> (path, pts |> Seq.map snd))
    seq {
        for (path, ts) in pathTimestampsPairs do
             printfn "%s %d" path (Seq.length ts)
             let byMonth = ts |> Seq.groupBy (fun t -> (firstDayOfMonth t))
             for month, monthEdits in byMonth do
                   yield month, path, (estimatedEditTime monthEdits)
        }


let printFileEditTimesByMonth events =
    for month, path, duration in (estimatedEditTimeByFileByMonth events |> Seq.sort) do
        printfn "%s\t%s\t%.0f minutes" (month.ToShortDateString()) path (duration.TotalMinutes)


let filename =
    match fsi.CommandLineArgs with
    | [|_; logfile|] ->
        logfile
    | _ ->
        ""

match parseActivityLog filename with
| Some error, _ ->
    printfn "** ERROR: %s" error
| None, events ->
    printFileEditTimesByMonth events


