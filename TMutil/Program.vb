Imports System
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Web.HttpUtility
Module Program
    Private T As TM_Client
    Private tf_components As List(Of tmComponent)
    Sub Main(args As String())

        Dim actionWord$ = ""

        actionWord$ = args(0)

        If args.Count.ToString < 1 Or argExist("help", args) Then
            Call giveHelp()
            End
        End If

        Dim fqD$ = argValue("fqdn", args)
        Dim usR$ = argValue("un", args)
        Dim pwD$ = argValue("pw", args)

        If actionWord = "makelogin" Then
            If fqD = "" Or usR = "" Or pwD = "" Then
                Console.WriteLine("Must provide all 3 arguments: FQDN, UN and PW")
            End If

            Console.WriteLine(usR + "|" + pwD)
            Call addLoginCreds(fqD, usR, pwD)
            Console.WriteLine("Credentials added to login file - note only the first set of credentials is used")
            End
        End If

        Dim lgnS = getLogins()
        If lgnS.Count = 0 And fqD + usR + pwD = "" Then
            Console.WriteLine("Make sure you have a login file or specify FQDN,UN,PW - with login file, the first entry will be used to log in")
            End
        End If

        If fqD + usR + pwD = "" Then
            usR = loginItem(lgnS(1), 1)
            pwD = loginItem(lgnS(1), 2)
            fqD = "https://" + loginItem(lgnS(1), 0) ', "")
        Else
            fqD = "https://" + fqD
        End If
        '''''''' Console.WriteLine(uN + "|" + pW)

        If fqD <> "" And lgnS.Count Then
            ' user providing FQDN but not credentials - look to file
            T = returnTMClient(fqD)
        Else
            T = New TM_Client(fqD, usR, pwD)
        End If

        If T.isConnected = True Then
            '            addLoginCreds(fqdN, uN, pW)
            Console.WriteLine("Bearer Token Obtained/ Client connection established")
        Else
            End
        End If

        Console.WriteLine(vbCrLf + "ACTION: " + actionWord + vbCrLf)

        Dim GRP As List(Of tmGroups)

        Select Case LCase(actionWord)
            Case "compliance_csv"
                Dim cFrameworks As List(Of complianceDetails) = T.getCompFrameworks

                Dim filterS$ = argValue("framework", args)
                Dim fileN$ = argValue("file", args)
                Dim summaryOnly As Boolean = False
                If LCase(argValue("summary", args)) = "true" Then summaryOnly = True

                Dim FF As Integer

                If Len(fileN) Then
                    safeKILL(fileN)
                    FF = FreeFile()
                    Call loadNTY(T, "SecurityRequirements")
                    Console.WriteLine("Writing to CSV File: " + fileN)
                    FileOpen(FF, fileN, OpenMode.Output)
                End If

                Dim c$ = Chr(34)

                For Each F In cFrameworks
                    If Len(filterS) Then
                        If LCase(F.Name) <> LCase(filterS) Then GoTo skipIt
                    End If

                    If Len(fileN) Then
                        '                        WriteLine()
                    Else
                        Console.WriteLine(col5CLI(F.Name + " [" + F.Id.ToString + "]", "", "", ""))
                    End If
                    For Each S In F.Sections
                        If Len(fileN) Then
                            Dim newLine$ = F.Id.ToString + "," + c + F.Name + c + ","
                            Dim cFam$ = S.Domain
                            If InStr(LCase(F.Name), "nist") Then cFam = Mid(S.Name, 1, 2)

                            newLine += c + cFam + c + "," + S.Id.ToString + "," + c + S.Name + c + "," + c + S.Title + c + ","

                            If summaryOnly = True Then
                                Print(FF, newLine + S.SecurityRequirements.Count.ToString + vbCrLf)
                            Else
                                For Each SR In S.SecurityRequirements
                                    Dim srName$ = "UNDEFINED"
                                    srName = T.lib_SR(T.ndxSRlib(SR.Id)).Name
                                    Print(FF, newLine + SR.Id.ToString + "," + c + srName + c + ",1" + vbCrLf)
                                Next
                            End If
                        Else
                                Console.WriteLine(col5CLI("", S.Name + " [" + S.Id.ToString + "]", S.Title, "   ", S.SecurityRequirements.Count.ToString))
                        End If
                    Next
skipIt:
                Next

                If Len(fileN) Then FileClose(FF)
                End


            Case "submitkiss"
                Dim modelName$ = argValue("modelname", args)
                If modelName = "" Then
                    Console.WriteLine("You must provide a name for the Model using --MODELNAME (name)")
                    End
                End If

                Dim modelNum As Integer
                Dim result2$ = T.createKISSmodelForImport(modelName)
                modelNum = Val(result2)
                If modelNum = 0 Then
                    Console.WriteLine("Unable to create project - " + result2)
                    End
                End If

                Console.WriteLine("Submitting JSON for import into Project #" + modelNum.ToString)

                Dim fileN$ = argValue("file", args)
                If Dir(fileN) = "" Then
                    Console.WriteLine("file does not exist " + fileN)
                End If

                Dim resP$ = T.importKISSmodel(fileN, modelNum)
                If Mid(resP, 1, 5) = "ERROR" Then
                    Console.WriteLine(resP)
                Else
                    Console.WriteLine(T.tmFQDN + "/diagram/" + modelNum.ToString)
                End If
                End


            Case "submitcfn"
                Dim modelName$ = argValue("modelname", args)
                modelName = Now.Ticks.ToString + modelName

                Dim modelNum As Integer
                Dim result2$ = T.createKISSmodelForImport(modelName)
                modelNum = Val(result2)
                If modelNum = 0 Then
                    Console.WriteLine("Unable to create project - " + result2)
                    End
                End If

                Console.WriteLine("Submitting JSON for import into Project #" + modelNum.ToString)

                Dim fileN$ = argValue("file", args)
                If Dir(fileN) = "" Then
                    Console.WriteLine("file does not exist " + fileN)
                End If

                Dim resP$ = T.importKISSmodel(fileN, modelNum, "CloudFormation")
                If Mid(resP, 1, 5) = "ERROR" Then
                    Console.WriteLine(resP)
                Else
                    Console.WriteLine(T.tmFQDN + "/diagram/" + modelNum.ToString)
                End If
                End





            Case "appscan"
                Dim sDir$ = argValue("dir", args)
                Dim block$ = argValue("block", args)

                Dim publicOnly As Boolean = False
                Dim classesOnly As Boolean = False

                Dim modelType = argValue("type", args)

                Dim showOnlyFiles$ = argValue("onlyfiles", args)
                Dim showClients As Boolean = True
                If LCase(argValue("showclients", args)) = "false" Then showClients = False

                Dim objectsToWatch$ = argValue("objectwatch", args)
                Dim maxdepth As Integer = Val(argValue("depth", args))

                Call loadNTY(T, "Components")
                Dim bestMethod As Integer = T.ndxCompbyName("Method")
                Dim bestRMethod As Integer = T.ndxCompbyName("Return Method")
                Dim bestCC As Integer = T.ndxCompbyName("Code Collection")
                Dim bestCL As Integer = T.ndxCompbyName("Class")
                Dim bestSF As Integer = T.ndxCompbyName("Source File")

                Dim nScan As New appScan

                '                Console.WriteLine("Found AppScan Components:")
                If bestMethod <> -1 Then
                    '                   Console.WriteLine("Method: " + T.lib_Comps(bestMethod).Guid.ToString)
                    nScan.bestMethod = T.lib_Comps(bestMethod).Guid.ToString
                Else
                    Console.WriteLine("Cannot find component 'Method'")
                    End
                End If
                If bestRMethod <> -1 Then
                    '                  Console.WriteLine("Return Method: " + T.lib_Comps(bestRMethod).Guid.ToString)
                    nScan.bestRMethod = T.lib_Comps(bestRMethod).Guid.ToString
                Else
                    Console.WriteLine("Cannot find component 'Return Method'")
                    End
                End If
                If bestCC <> -1 Then
                    '                 Console.WriteLine("Code Collection " + T.lib_Comps(bestCC).Guid.ToString)
                    nScan.bestCC = T.lib_Comps(bestCC).Guid.ToString
                Else
                    Console.WriteLine("Cannot find component 'Code Collection'")
                    End
                End If
                If bestCL <> -1 Then
                    '                Console.WriteLine("Class " + T.lib_Comps(bestCL).Guid.ToString)
                    nScan.bestCL = T.lib_Comps(bestCL).Guid.ToString
                Else
                    Console.WriteLine("Cannot find component 'Class'")
                    End
                End If

                'some change here


                ' some other change


                If bestSF <> -1 Then
                    '               Console.WriteLine("Source File " + T.lib_Comps(bestSF).Guid.ToString)
                    nScan.bestSF = T.lib_Comps(bestSF).Guid.ToString
                Else
                    Console.WriteLine("Cannot find component 'Source File'")
                    End
                End If

                Console.WriteLine("Scanning for classes And methods")
                Dim resulT1$ = ""
                resulT1 = nScan.doScan(sDir, block, objectsToWatch, LCase(modelType), showOnlyFiles, maxdepth, showClients)

                If resulT1 = "ERROR" Or Dir(resulT1) = "" Then
                    Console.WriteLine("Unable to create JSON file")
                    If Dir(resulT1) = "" Then Console.WriteLine("File Not found: " + resulT1)
                    End
                End If



                Dim modelName$ = Now.Ticks.ToString + "_" + argValue("modelname", args)
                If modelName = "" Then
                    Console.WriteLine("You must provide a name for the Model using --MODELNAME (name)")
                    End
                End If
                '                modelName = Now.Ticks.ToString + modelName

                Dim modelNum As Integer
                Dim result2$ = T.createKISSmodelForImport(modelName)
                modelNum = Val(result2)
                If modelNum = 0 Then
                    Console.WriteLine("Unable to create project - " + result2)
                    End
                End If

                Console.WriteLine(vbCrLf + "Submitting JSON for import into Project #" + modelNum.ToString)

                Dim resP$ = T.importKISSmodel(resulT1, modelNum)
                If Mid(resP, 1, 5) = "ERROR" Then
                    Console.WriteLine(resP)
                Else
                    Console.WriteLine(T.tmFQDN + "/diagram/" + modelNum.ToString)
                End If
                'Console.WriteLine(T.tmFQDN + "/diagram/" + modelNum.ToString)
                End

            Case "summary"
                GRP = T.loadAllGroups(T, True)
                End


            Case "fix_templates"
                Dim fileN$ = argValue("file", args)
                Dim outF$ = argValue("fileout", args)

                If Dir(fileN) = "" Or fileN = "" Then
                    Console.WriteLine("Use --FILE to specify filename of CSV containing templates")
                    End
                End If

                If outF = "" Then
                    Console.WriteLine("Use --FILEOUT to specify filename of SQL output")
                End If

                Dim nextLine$ = ""
                Dim getLine$ = ""
                Dim FF As Integer = 0
                Dim FN As Integer = 0
                FF = FreeFile()

                FileOpen(FF, fileN, OpenMode.Input)

                safeKILL(outF)
                FN = FreeFile()
                FileOpen(FN, outF, OpenMode.Output)

newline:
                If EOF(FF) = True Then GoTo doneHere
                nextLine = Replace(LineInput(FF), vbCrLf, "")

                If Len(nextLine) > 3 And Len(getLine) Then
                    If Val(Mid(nextLine, 1, 3)) > 1 Then
                        PrintLine(FN, Replace(getLine, vbCrLf, ""))
                        getLine = ""
                        'GoTo newline
                    End If
                End If

                getLine += nextLine
                If EOF(FF) = False Then GoTo newline
doneHere:

                PrintLine(FN, getLine)

                FileClose(FF)
                FileClose(FN)
                End

            Case "templatecsv"
                Dim fileN$ = argValue("file", args)
                Dim outF$ = argValue("fileout", args)

                If Dir(fileN) = "" Or fileN = "" Then
                    Console.WriteLine("Use --FILE To specify filename Of CSV containing templates")
                    End
                End If

                If outF = "" Then
                    Console.WriteLine("Use --FILEOUT To specify filename Of SQL output")
                End If

                Dim csvLine$ = ""
                Dim FF As Integer = 0
                Dim FN As Integer = 0
                FF = FreeFile()

                FileOpen(FF, fileN, OpenMode.Input)

                safeKILL(outF)
                FN = FreeFile()
                FileOpen(FN, outF, OpenMode.Output)

                Dim c$
                c$ = Chr(34) + Chr(34)

                Do Until EOF(FF) = True
                    csvLine = LineInput(FF)

                    'Id,Name,Type,Json,Description,Labels,Image,DiagramData,Guid,LibraryId,isHidden,LastUpdated
                    'Id	Name	Json	DiagramData	Guid	LibraryId	isHidden

                    Dim LOI As Object
                    LOI = Split(csvLine, vbTab)

                    Dim sqL$ = "INSERT INTO Templates (Name,Json,DiagramData,GUID,LibraryId,isHidden,LastUpdated) VALUES ("

                    ' 1=name,3=json,6=image,7=diagramdata,8=guid,9=libraryID
                    Dim colNum As Integer = 0

                    For Each L In LOI
                        If colNum > 0 Then
                            If colNum = 3 Or colNum = 5 Or colNum = 6 Then sqL += L + ","
                            If colNum = 1 Or colNum = 2 Or colNum = 4 Then sqL += "'" + L + "',"
                        End If
                        colNum += 1
                    Next

                    sqL += "'2022-02-25 2:22:22')"
                    PrintLine(FN, sqL)
                    'Console.WriteLine(sqL)
                Loop

                FileClose(FF)
                FileClose(FN)
                End

            Case "get_notesDEPRECATE"
                GRP = T.loadAllGroups(T)
                For Each G In GRP
                    For Each PI In G.AllProjInfo
                        Dim NN As New List(Of tmNodeData)

                        For Each nodeD In PI.Model.Nodes
                            If Len(nodeD.Notes) And nodeD.Notes <> "0" Then
                                NN.Add(nodeD)
                            End If
                        Next
                        If NN.Count Then
                            Console.WriteLine("---------------------------------------------")
                            Console.WriteLine(">>>>> Project: " + PI.Name + " [" + PI.Id.ToString + "]")
                            For Each N In NN
                                Console.WriteLine(N.ComponentName + " (" + N.ComponentTypeName + "): " + N.Notes)
                            Next
                            Console.WriteLine("---------------------------------------------")
                        End If
                    Next
                Next
                End

            Case "serialize"
                Dim R As New threatStatusUpdate
                With R
                    .Id = 28012
                    .ThreatId = 172202
                    .StatusId = 2
                    .ProjectId = 2431
                End With
                Console.WriteLine(T.updateThreatStat(R))
                End


            Case "get_labels"
                Dim LL As List(Of tmLabels)
                Dim isS As Boolean = False
                Console.WriteLine("ARG:" + argValue("issystem", args))
                If argValue("issystem", args) = "" Then
                    Console.WriteLine("ERROR: Must include IsSystem boolean.. eg get_labels --isSystem false")
                    End
                End If
                If LCase(argValue("issystem", args)) = "true" Then isS = True Else isS = False
                LL = T.getLabels(isS)

                Dim srchS$ = argValue("search", args)
                If Len(srchS) Then srchS = LCase(srchS)

                Dim numItems As Integer = 0

                For Each L In LL
                    If Len(srchS) Then
                        If InStr(LCase(L.Name), LCase(srchS)) = 0 Then GoTo skipMeLabel
                    End If
                    numItems += 1
                    Dim a$ = "System:"
                    If L.IsSystem = True Then a += "True" Else a += "False"
                    Console.WriteLine(fLine("Id:" + L.Id.ToString + " " + a, L.Name))
skipMeLabel:

                Next

                Console.WriteLine("# of Items: " + numItems.ToString)
                End


            Case "get_threats", "get_threat"
                Dim R As New tfRequest

                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With

                T.lib_TH = T.getTFThreats(R)
                Console.WriteLine("Loaded threats: " + T.lib_TH.Count.ToString)

                R.EntityType = "Components"
                T.lib_Comps = T.getTFComponents(R)
                Console.WriteLine("Loaded comps: " + T.lib_Comps.Count.ToString)

                T.librarieS = T.getLibraries
                Console.WriteLine("Loaded libraries: " + T.librarieS.Count.ToString)

                Dim srchS$ = argValue("search", args)
                If Len(srchS) Then srchS = LCase(srchS)

                Dim numItems As Integer = 0

                If Val(argValue("id", args)) = 0 Then
                    Dim doCSV As Boolean
                    Dim fileN$ = argValue("file", args)
                    Dim FF As Integer

                    If Len(fileN) Then
                        doCSV = True
                        safeKILL(fileN)
                        FF = FreeFile()
                        Console.WriteLine("Writing to CSV File: " + fileN)
                        FileOpen(FF, fileN, OpenMode.Output)
                        Print(FF, "Library,Name,ID,Risk,Labels" + vbCrLf)
                    End If

                    Dim qq$ = Chr(34)


                    For Each P In T.lib_TH
                        If Len(srchS) Then
                            If InStr(LCase(P.Labels), srchS) Then GoTo showTH
                            If InStr(LCase(P.Name), srchS) Then GoTo showTH
                            'If InStr(LCase(P.Description), srchS) Then GoTo showTH
                            GoTo skipTH
                        End If
showTH:
                        Dim libNDX As Integer = T.ndxLib(P.LibraryId)
                        Dim lName$ = ""
                        If libNDX <> -1 Then lName$ = T.librarieS(libNDX).Name

                        lName += " [" + P.LibraryId.ToString + "]"
                        lName += spaces(30 - Len(lName))

                        Dim idSt$ = "[" + P.Id.ToString + "]"

                        Console.WriteLine(lName + spaces(30 - Len(lName)) + idSt + spaces(10 - Len(idSt)) + P.Name) ' P.CreatedByName + Space(30 - Len(P.CreatedByName)), " ", "Vers " + P.Version))

                        If doCSV Then
                            Print(FF, qq + lName + qq + "," + qq + P.Name + qq + "," + P.Id.ToString + "," + qq + P.RiskName + qq + "," + qq + P.Labels + qq)
                        End If
                        numItems += 1
skipTH:
                    Next
                    Console.WriteLine("# of items in this list: " + numItems.ToString)
                    If doCSV Then
                        FileClose(FF)
                    End If
                    End
                Else


                    Dim tNDX As Integer = T.ndxTHlib(Val(argValue("id", args)))
                    If tNDX = -1 Then
                        Console.WriteLine("NOT FOUND: Threat ID " + argValue("id", args).ToString)
                        End
                    End If

                    R.EntityType = "SecurityRequirements"
                    T.lib_SR = T.getTFSecReqs(R)
                    Console.WriteLine("Loaded SRs: " + T.lib_SR.Count.ToString)

                    Call T.defineTransSRs(T.lib_TH(tNDX).Id) '.Id)

                    With T.lib_TH(tNDX)
                        Console.WriteLine(vbCrLf + "NAME       :" + .Name + spaces(80 - Len(.Name)) + "[" + .Id.ToString + "] # SR: " + .listLinkedSRs.Count.ToString)
                        Console.WriteLine("LIBRARY    :" + T.librarieS(T.ndxLib(.LibraryId)).Name) '.ToString')
                        Console.WriteLine("GUID       :" + .Guid.ToString)     ')

                        Console.WriteLine("LABELS     :" + .Labels)
                        Console.WriteLine("DESCRIPTION:" + .Description)

                        Console.WriteLine(vbCrLf)
                        Console.WriteLine("REQUIREMENTS: " + .listLinkedSRs.Count.ToString)

                        For Each S In .listLinkedSRs
                            Dim sNdx As Integer = T.ndxSRlib(S)
                            Dim idSt$ = "[" + T.lib_SR(sNdx).Id.ToString + "]"

                            Console.WriteLine(idSt + spaces(10 - Len(idSt)) + T.lib_SR(sNdx).Name) ' P.CreatedByName + Space(30 - Len(P.CreatedByName)), " ", "Vers " + P.Version))

                        Next
                        Console.WriteLine("=================================")

                    End With

                    Console.WriteLine("Components that use this Threat (this is slow - ^C to abort)")
                    Console.WriteLine("=================================")

                    Dim numComps As Integer = 0
                    For Each cc In T.lib_Comps
                        Call T.buildCompObj(cc, True)
                        If T.ndxTHofList(T.lib_TH(tNDX).Id, cc.listThreats) <> -1 Then
                            Console.WriteLine(cc.Name + " [" + cc.Id.ToString + "]")
                            numComps += 1
                        End If
                    Next
                    Console.WriteLine("# of Components with this Threat: " + numComps.ToString)
                End If

            Case "get_sr"
                Dim R As New tfRequest

                With R
                    .EntityType = "SecurityRequirements"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_SR = T.getTFSecReqs(R)

                T.librarieS = T.getLibraries
                Console.WriteLine("Loaded libraries: " + T.librarieS.Count.ToString)

                Dim srchS$ = argValue("search", args)
                If Len(srchS) Then srchS = LCase(srchS)

                Dim numItems As Integer = 0

                If Val(argValue("id", args)) = 0 Then
                    Dim doCSV As Boolean
                    Dim fileN$ = argValue("file", args)
                    Dim FF As Integer

                    If Len(fileN) Then
                        doCSV = True
                        safeKILL(fileN)
                        FF = FreeFile()
                        Console.WriteLine("Writing to CSV File: " + fileN)
                        FileOpen(FF, fileN, OpenMode.Output)
                    End If

                    Dim qq$ = Chr(34)

                    If doCSV Then
                        Print(FF, "Library,Name,ID,Labels" + vbCrLf)
                    End If

                    For Each P In T.lib_SR
                        If Len(srchS) Then
                            If InStr(LCase(P.Labels), srchS) Then GoTo showSR
                            If InStr(LCase(P.Name), srchS) Then GoTo showSR
                            GoTo skipSR
                        End If
showSR:
                        Dim libNDX As Integer = T.ndxLib(P.LibraryId)
                        Dim lName$ = ""
                        If libNDX <> -1 Then lName$ = T.librarieS(libNDX).Name

                        lName += " [" + P.LibraryId.ToString + "]"
                        lName += spaces(30 - Len(lName))

                        Dim idSt$ = "[" + P.Id.ToString + "]"

                        Console.WriteLine(lName + spaces(30 - Len(lName)) + idSt + spaces(10 - Len(idSt)) + P.Name) ' P.CreatedByName + Space(30 - Len(P.CreatedByName)), " ", "Vers " + P.Version))

                        If doCSV Then
                            Print(FF, qq + lName + qq + "," + qq + P.Name + qq + "," + P.Id.ToString + "," + qq + P.Labels + qq + vbCrLf)
                        End If
                        numItems += 1
skipSR:
                    Next
                    Console.WriteLine("# of items in this list: " + numItems.ToString)
                    If doCSV Then
                        FileClose(FF)
                    End If

                    End
                Else

                    Dim sNDX As Integer = T.ndxSRlib(Val(argValue("id", args)))
                    If sNDX = -1 Then
                        Console.WriteLine("NOT FOUND: Security Requirement ID " + argValue("id", args).ToString)
                        End
                    End If
                    With T.lib_SR(sNDX)
                        Console.WriteLine("REQUIREMENT : [" + .Id.ToString + "]" + spaces(10 - Len(.Id.ToString)) + " " + .Name)
                        Console.WriteLine("LIBRARY    :" + T.librarieS(T.ndxLib(.LibraryId)).Name) '.ToString')
                        Console.WriteLine("GUID       :" + .Guid.ToString)     ')

                        Console.WriteLine("DESCRIPTION :" + vbCrLf + .Description)
                        Console.WriteLine("LABELS      :" + .Labels)

                    End With

                    R.EntityType = "Threats"
                    T.lib_TH = T.getTFThreats(R)
                    'Console.WriteLine("Loaded threats: " + T.lib_TH.Count.ToString)

                    Console.WriteLine("Threats that use this SR (this is slow - ^C to abort)")
                    Console.WriteLine("=================================")

                    For Each tT In T.lib_TH
                        Call T.defineTransSRs(tT.Id, True)
                        If grpNDX(tT.listLinkedSRs, T.lib_SR(sNDX).Id) Then
                            Console.WriteLine("        THREAT : [" + tT.Id.ToString + "] " + spaces(10 - Len(tT.Id.ToString)) + tT.Name + " [" + tT.listLinkedSRs.Count.ToString + " SRs Total]")
                        End If
                    Next
                    '                    Console.WriteLine("# of Components with this Threat: " + numComps.ToString)

                End If
                End

            Case "get_groups"
                Dim GG As List(Of tmGroups)
                GG = T.getGroups()
                For Each G In GG
                    Console.WriteLine(fLine("Id:" + G.Id.ToString, G.Name))
                Next
                End


            Case "get_users"
                Dim deptId As Integer = 0
                Dim allDepts As List(Of tmDept) = T.getDepartments
                Console.WriteLine("# of Departments: " + allDepts.Count.ToString)

                Dim numDays As Integer = 0
                If Len(argValue("lastlogin", args)) Then numDays = Val(argValue("lastlogin", args))
                If Len(argValue("dept", args)) Then deptId = Val(argValue("dept", args))

                Dim allUsers As List(Of tmUser) = New List(Of tmUser)
                Dim doCSV As Boolean
                Dim fileN$ = argValue("file", args)
                Dim FF As Integer

                If Len(fileN) Then
                    doCSV = True
                    safeKILL(fileN)
                    FF = FreeFile()
                    Console.WriteLine("Writing to CSV File: " + fileN)
                    FileOpen(FF, fileN, OpenMode.Output)
                End If

                Dim c$ = Chr(34)

                If doCSV Then
                    Print(FF, "Department,DeptId,Name,UserId,Email,Username,Active,LastLogin" + vbCrLf)
                End If

                For Each D In allDepts
                    If deptId Then
                        If deptId <> D.Id Then GoTo skipDept1
                    End If
                    allUsers = T.getUsers(D.Id)
                    For Each P In allUsers
                        If P.DepartmentId <> D.Id Then GoTo skipUser1

                        Dim LLI$ = ""
                        If IsNothing(P.LastLogin) = False Then
                            LLI = CDate(P.LastLogin).ToShortDateString
                        End If

                        Dim actualDays As Integer = 0

                        If Len(LLI) Then actualDays = DateDiff("d", CDate(LLI), CDate(Today))

                        If numDays > 0 And Len(LLI) > 1 Then
                            If actualDays < numDays Then
                                GoTo skipUser1
                            End If
                        End If
                        If numDays And Len(LLI) = 0 Then GoTo skipUser1

                        Dim active$ = "Active"
                        If P.Activated = False Then active = "Not Active"

                        Console.WriteLine(col5CLI(D.Name + " [" + D.Id.ToString + "]", P.Name + " [" + P.Id.ToString + "]", P.Email, spaces(50 - Len(P.Email)) + active, spaces(15 - Len(active)) + LLI + " # Days: " + actualDays.ToString))

                        If doCSV = False Then GoTo skipUser1
                        With P
                            Print(FF, c + D.Name + c + "," + D.Id.ToString + "," + c + P.Name + c + "," + P.Id.ToString + "," + c + P.Email + c + "," + c + P.Username + c + "," + c + active + c + "," + c + LLI + c + vbCrLf)
                        End With
skipUser1:
                    Next
skipDEPT1:
                Next

                If doCSV = True Then
                    FileClose(FF)
                End If
                End

            Case "get_projects"
                Dim PP As List(Of tmProjInfo)
                PP = T.getAllProjects()

                Dim doCSV As Boolean = False
                Dim fileN$ = ""

                If Len(argValue("file", args)) Then
                    doCSV = True
                    fileN = argValue("file", args)
                    safeKILL(fileN)
                    Console.WriteLine("Writing CSV file -> " + fileN)

                End If
                Dim c$ = Chr(34)

                Dim FF As Integer

                If doCSV Then
                    FF = FreeFile()
                    FileOpen(FF, fileN, OpenMode.Output)
                    Print(FF, "Project Name,Id,Version,Create Date,Created By,Last Modified Date,Last Modified By,Labels" + vbCrLf)
                End If

                For Each P In PP
                    Console.WriteLine(col5CLI(P.Name + " [" + P.Id.ToString + "]", P.Type, P.CreatedByName + Space(30 - Len(P.CreatedByName)), " ", "Vers " + P.Version))

                    If doCSV = False Then GoTo skipCSV
                    With P
                        Print(FF, c$ + .Name + c$ + "," + .Id.ToString + "," + c$ + .Version + c$ + "," + c$ + .CreateDate.ToShortDateString + c$ + "," + c$ + .CreatedByName + c$ + "," + c$ + .LastModifiedDate.ToShortDateString + c$ + "," + c$ + .LastModifiedByName + c$ + "," + c$ + .Labels + c$ + vbCrLf)
                    End With
skipCSV:
                Next

                If doCSV = True Then
                    FileClose(FF)
                End If
                End


            Case "show_aws_iam"
                Dim awsACC As List(Of tmAWSacc)
                awsACC = T.getAWSaccounts()
                For Each A In awsACC
                    Console.WriteLine(A.Id.ToString + spaces(10 - Len(A.Id.ToString)) + A.Name)
                Next

            Case "create_vpc_model"
                Dim vpcID As Long = Val(argValue("vpcid", args))
                If Len(argValue("vpcid", args)) = 0 Then
                    Console.WriteLine("Must provide valid VPC ID value using arg --vpcid")
                    End
                End If
                Dim awsACC As List(Of tmAWSacc)
                awsACC = T.getAWSaccounts()

                For Each A In awsACC
                    Dim vpcS As List(Of tmVPC)
                    'Console.WriteLine("Searching ID " + A.Id.ToString + " " + A.Name)
                    vpcS = T.getVPCs(A.Id)

                    'Console.WriteLine("Looking for VPC ID " + vpcID.ToString + " inside " + A.Name + " (" + vpcS.Count.ToString + " VPCs)")

                    Dim ndX As Integer = T.ndxVPC(vpcID, vpcS)
                    If ndX = -1 Then GoTo nextOne

                    'here is VPC
                    With vpcS(ndX)
                        Console.WriteLine("Found " + vpcID.ToString + " [" + .VPCId + "] inside account " + A.Id.ToString + "-" + A.Name)

                        If IsNothing(.Version) = False Then
                            Console.WriteLine("A model already exists for this VPC (" + T.tmFQDN + "/diagram/" + .ProjectId.ToString + ")")
                            End
                        End If

                        .Version = Date.Now.ToString("MdyyyyHHmm")
                        .ProjectName = .VPCId
                        .Type = "AWSCloudApplication"
                        .Labels = "AWS"
                        .GroupPermissions = New List(Of String)
                        .UserPermissions = New List(Of String)
                    End With

                    Console.WriteLine("Sending command to create threat model from:" + vbCrLf + T.prepVPCmodel(vpcS(ndX)))
                    Call T.createVPCmodel(vpcS(ndX))
                    End

nextOne:
                Next
                Console.WriteLine("No VPC Id " + vpcID.ToString + " found")
                End


            Case "show_vpc"
                Dim awsID As Long = Val(argValue("awsid", args))
                If awsID = 0 Then
                    Console.WriteLine("Must provide valid AWS account ID value using arg --awsid")
                    End
                End If
                Dim vpcS As List(Of tmVPC)
                vpcS = T.getVPCs(awsID)

                If IsNothing(vpcS) Or vpcS.Count = 0 Then
                    Console.WriteLine("VPC not found")
                End If

                Console.WriteLine(col5CLI("VPC Name/ID", "Version Datestamp", "DRIFT/VPC", "DRIFT/TM")) ', vVers, vLast, vDriftVPC, vDriftTM))
                Console.WriteLine(col5CLI("===========", "=================", "=========", "========")) ', vVers, vLast, vDriftVPC, vDriftTM))

                For Each V In vpcS
                    Dim vVers$
                    Dim vLast$
                    Dim vDriftVPC$ = ""
                    Dim vDriftTM$ = ""
                    If IsNothing(V.Notes) = False Then
                        If InStr(V.Notes, "Following Nodes are available in AWS VPC") Then vDriftVPC = "TRUE" Else vDriftVPC = "FALSE"
                        If InStr(V.Notes, "Following Nodes are available in Threat Model") Then vDriftTM = "TRUE" Else vDriftTM = "FALSE"
                    End If
                    If IsNothing(V.Version) = False Then
                        vVers = V.Version
                    Else
                        vVers = "Not Created"
                        vDriftVPC = ""
                        vDriftTM = ""
                    End If

                    If IsNothing(V.LastSync) = False Then vLast = V.LastSync.ToString Else vLast = ""
                    Console.WriteLine(col5CLI(V.VPCId + " [" + V.Id.ToString + "]", vVers, vDriftVPC, vDriftTM))
                Next

            Case "attr_report"
                ' shows all entity types and # of threats

                Dim fileN$ = ""
                fileN = argValue("file", args)

                Dim FF As Integer = 0

                If Len(fileN) Then
                    safeKILL(fileN)
                    FF = FreeFile()
                    FileOpen(FF, fileN, OpenMode.Output)
                End If

                T.librarieS = T.getLibraries

                Dim R As tfRequest
                R = New tfRequest

                Console.WriteLine("Test")
                With R
                    .EntityType = "Attributes"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_AT = T.getTFAttr(R)

                Dim c$ = Chr(34)

                R.EntityType = "DataElements"
                Dim TFdataElements As New List(Of tmMiscTrigger)
                TFdataElements = T.getEntityMisc(R)
                R.EntityType = "Roles"
                Dim TFroles As New List(Of tmMiscTrigger)
                TFroles = T.getEntityMisc(R)
                R.EntityType = "Widgets"
                Dim TFwidgets As New List(Of tmMiscTrigger)
                TFwidgets = T.getEntityMisc(R)

                Dim a$ = ""

                For Each D In TFdataElements
                    Dim TH As List(Of tmProjThreat) = T.threatsOfEntity(D, "DataElements")
                    a$ = "DataElement,DataType," + D.Name + "," + TH.Count.ToString + ",1"
                    Console.WriteLine(a$)
                    If FF Then PrintLine(FF, a)
                Next
                For Each tfR In TFroles
                    Dim TH As List(Of tmProjThreat) = T.threatsOfEntity(tfR, "Roles")
                    a$ = "Roles,Role," + tfR.Name + "," + TH.Count.ToString + ",1"
                    Console.WriteLine(a$)
                    If FF Then PrintLine(FF, a)
                Next
                For Each W In TFwidgets
                    Dim widgetThreats As List(Of tmBackendThreats) = T.threatsByBackend(W)

                    Dim backendScounteD As New Collection

                    For Each B In widgetThreats
                        If grpNDX(backendScounteD, B.BackendId) = 0 Then
                            a$ = "Widget," + W.Name + "," + B.BackendName + "," + numWidgetThreats(widgetThreats, B.BackendId).ToString + ",1"
                            Console.WriteLine(a$)
                            If FF Then PrintLine(FF, a)
                            backendScounteD.Add(B.BackendId)
                        End If
                    Next
                Next
                For Each AT In T.lib_AT
                    For Each O In AT.Options
                        Dim aName$ = c + "[" + AT.Id.ToString + "] " + spaces(4 - Len(AT.Id.ToString)) '+ Replace(AT.Name, vbCrLf, "")
                        If Len(AT.Name) > 80 Then aName += Mid(AT.Name, 1, 78) + ".." + c Else aName += AT.Name + c
                        Dim oName$ = c + O.Name + c
                        a$ = "Attribute," + aName + "," + oName + "," + O.Threats.Count.ToString + ",1"
                        Console.WriteLine(a$)
                        If FF Then PrintLine(FF, a)
                    Next
                Next

                If FF Then FileClose(FF)
                End


            Case "addattr_threat"
                Dim cID As Integer = Val(argValue("id", args))
                Dim thrID As Integer = Val(argValue("threatid", args))

                If cID = 0 Then
                    Console.WriteLine("Must provide either an --ID Or --NAME argument to identify component of interest")
                    End
                End If

                If thrID = 0 Then
                    Console.WriteLine("Must provide the --THREATID of the Threat you'd like to add")
                    End
                End If

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Attributes"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_AT = T.getTFAttr(R)

                Dim ndxC As Integer

                Dim C As tmAttribute

                ndxC = T.ndxATTR(cID)
                If ndxC = -1 Then
                    Console.WriteLine("Attribute does not exist")
                    End
                End If
                Console.WriteLine("Found Attribute " + cID.ToString + ": " + T.lib_AT(ndxC).Name)
                C = T.lib_AT(ndxC)

                R.EntityType = "Threats"
                T.lib_TH = T.getTFThreats(R)

                'Console.WriteLine("Loaded threats: " + T.lib_TH.Count.ToString)
                Dim ndxT As Integer = 0
                ndxT = T.ndxTHlib(thrID)
                If ndxT = -1 Then
                    Console.WriteLine("Threat does not exist")
                    End
                End If
                Call T.defineTransSRs(T.lib_TH(ndxT).Id)

                Console.WriteLine("      THREAT: [" + T.lib_TH(ndxT).Id.ToString + "] " + T.lib_TH(ndxT).Name + " - # Linked SRs: " + T.lib_TH(ndxT).listLinkedSRs.Count.ToString)

                Call T.addThreatToAttribute(C, thrID)
                Console.WriteLine("Added Threat [" + T.lib_TH(ndxT).Id.ToString + "] to Attribute [" + C.Id.ToString + "] ")
                End


            Case "addcomp_threat"
                Dim cID As Integer = Val(argValue("id", args))
                Dim cNAME = argValue("name", args)
                Dim thrID As Integer = Val(argValue("threatid", args))

                If cID = 0 And cNAME = "" Then
                    Console.WriteLine("Must provide either an --ID Or --NAME argument to identify component of interest")
                    End
                End If

                If thrID = 0 Then
                    Console.WriteLine("Must provide the --THREATID of the Threat you'd like to add")
                    End
                End If

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)

                Dim ndxC As Integer

                Dim C As tmComponent

                If cID Then
                    ndxC = T.ndxComp(cID)
                    If ndxC = -1 Then
                        Console.WriteLine("Component does not exist")
                        End
                    End If
                    Console.WriteLine("Found Component " + cID.ToString + ": " + T.lib_Comps(ndxC).Name + " [" + T.lib_Comps(ndxC).CompID.ToString + "]")
                    C = T.lib_Comps(ndxC)
                Else
                    ndxC = T.ndxCompbyName(cNAME)
                    If ndxC = -1 Then
                        Console.WriteLine("Component does not exist")
                        End
                    End If
                    Console.WriteLine("Found Component: " + T.lib_Comps(ndxC).Name + " [" + T.lib_Comps(ndxC).CompID.ToString + "]")
                    C = T.lib_Comps(ndxC)
                End If

                R.EntityType = "Threats"
                T.lib_TH = T.getTFThreats(R)

                'Console.WriteLine("Loaded threats: " + T.lib_TH.Count.ToString)
                Dim ndxT As Integer = 0
                ndxT = T.ndxTHlib(thrID)
                If ndxT = -1 Then
                    Console.WriteLine("Threat does not exist")
                    End
                End If
                Call T.defineTransSRs(T.lib_TH(ndxT).Id)

                Console.WriteLine("      THREAT: [" + thrID.ToString + "] " + T.lib_TH(ndxT).Name + " - # Linked SRs: " + T.lib_TH(ndxT).listLinkedSRs.Count.ToString)

                R.EntityType = "SecurityRequirements"
                T.lib_SR = T.getTFSecReqs(R)



                Call addCompThreat(C, T.lib_TH(ndxT))
                Console.WriteLine("Threat Added to Component")
                End


            Case "addthreat_sr"
                Dim cID As Integer = Val(argValue("id", args))
                Dim srID As Integer = Val(argValue("srid", args))

                If cID = 0 Then
                    Console.WriteLine("Must provide an --ID to identify Threat of interest")
                    End
                End If

                If srID = 0 Then
                    Console.WriteLine("Must provide the --SRID (Requirement ID) of the SR you'd like to add")
                    End
                End If

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)

                Dim ndxC As Integer

                Dim C As tmProjThreat

                ndxC = T.ndxTHlib(cID)
                If ndxC = -1 Then
                    Console.WriteLine("Threat does not exist")
                    End
                End If

                C = T.lib_TH(ndxC)
                Console.WriteLine("Found Threat " + cID.ToString + ": " + C.Name)

                R.EntityType = "SecurityRequirements"
                T.lib_SR = T.getTFSecReqs(R)

                'Console.WriteLine("Loaded threats: " + T.lib_TH.Count.ToString)
                Dim ndxT As Integer = 0
                ndxT = T.ndxSRlib(srID)
                If ndxT = -1 Then
                    Console.WriteLine("Security Requirement does not exist")
                    End
                End If

                Call T.addSRtoThreat(C, srID)
                Console.WriteLine("SR [" + srID.ToString + "] Added to Threat [" + C.Id.ToString + "]: " + C.Name)
                End

            Case "subthreat_sr"

                Dim cID As Integer = Val(argValue("id", args))
                Dim srID As Integer = Val(argValue("srid", args))

                If cID = 0 Then
                    Console.WriteLine("Must provide an --ID to identify Threat of interest")
                    End
                End If

                If srID = 0 Then
                    Console.WriteLine("Must provide the --SRID (Requirement ID) of the SR you'd like to delete")
                    End
                End If

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)

                Dim ndxC As Integer

                Dim C As tmProjThreat

                ndxC = T.ndxTHlib(cID)
                If ndxC = -1 Then
                    Console.WriteLine("Threat does not exist")
                    End
                End If

                C = T.lib_TH(ndxC)
                Console.WriteLine("Found Threat " + cID.ToString + ": " + C.Name)

                R.EntityType = "SecurityRequirements"
                T.lib_SR = T.getTFSecReqs(R)

                'Console.WriteLine("Loaded threats: " + T.lib_TH.Count.ToString)
                Dim ndxT As Integer = 0
                ndxT = T.ndxSRlib(srID)
                If ndxT = -1 Then
                    Console.WriteLine("Security Requirement does not exist")
                    End
                End If

                Call T.addSRtoThreat(C, srID, True)
                Console.WriteLine("SR [" + srID.ToString + "] pulled off Threat [" + C.Id.ToString + "]: " + C.Name)
                End


            Case "addcomp_attr"
                Dim cID As Integer = Val(argValue("id", args))
                Dim cNAME = argValue("name", args)
                Dim attID As Integer = Val(argValue("attrid", args))

                If cID = 0 And cNAME = "" Then
                    Console.WriteLine("Must provide either an --ID Or --NAME argument to identify component of interest")
                    End
                End If

                If attID = 0 Then
                    Console.WriteLine("Must provide the --ATTRID of the Threat you'd like to add")
                    End
                End If

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)

                Dim ndxC As Integer

                Dim C As tmComponent

                If cID Then
                    ndxC = T.ndxComp(cID)
                    If ndxC = -1 Then
                        Console.WriteLine("Component does not exist")
                        End
                    End If
                    Console.WriteLine("Found Component " + cID.ToString + ": " + T.lib_Comps(ndxC).Name + " [" + T.lib_Comps(ndxC).CompID.ToString + "]")
                    C = T.lib_Comps(ndxC)
                Else
                    ndxC = T.ndxCompbyName(cNAME)
                    If ndxC = -1 Then
                        Console.WriteLine("Component does not exist")
                        End
                    End If
                    Console.WriteLine("Found Component: " + T.lib_Comps(ndxC).Name + " [" + T.lib_Comps(ndxC).CompID.ToString + "]")
                    C = T.lib_Comps(ndxC)
                End If

                R.EntityType = "Threats"
                T.lib_TH = T.getTFThreats(R)
                R.EntityType = "Attributes"
                T.lib_AT = T.getTFAttr(R)

                'Console.WriteLine("Loaded threats: " + T.lib_TH.Count.ToString)
                Dim ndxAT As Integer = 0
                ndxAT = T.ndxATTR(attID)
                If ndxAT = -1 Then
                    Console.WriteLine("Attribute does not exist")
                    End
                End If

                Dim idSt$ = "[" + T.lib_AT(ndxAT).Id.ToString + "]"
                Console.WriteLine("ATTRIBUTE :" + idSt + spaces(10 - Len(idSt)) + T.lib_AT(ndxAT).Name) ' P.CreatedByName + Space(30 - Len(P.CreatedByName)), " ", "Vers " + P.Version))
                Call T.addAttributeToComponent(C, T.lib_AT(ndxAT).Id)
                Console.WriteLine("========================")
                R.EntityType = "SecurityRequirements"
                T.lib_SR = T.getTFSecReqs(R)

                With T.lib_AT(ndxAT)
                    For Each O In .Options
                        Console.WriteLine("OPTION  : " + O.Name + "     #THR: " + O.Threats.Count.ToString)
                        For Each tt In O.Threats
                            idSt$ = "[" + tt.Id.ToString + "]"
                            Console.WriteLine("========================")
                            Console.WriteLine("THREAT    : " + idSt + spaces(10 - Len(idSt)) + tt.Name) ' P.CreatedByName + Space(30 - Len(P.CreatedByName)), " ", "Vers " + P.Version))
                            Call T.defineTransSRs(tt.Id)
                            For Each tsR In T.lib_TH(T.ndxTHlib(tt.Id)).listLinkedSRs
                                Dim srNDX As Integer = T.ndxSRlib(tsR)
                                Dim SR As tmProjSecReq = T.lib_SR(srNDX)
                                Dim lName$ = T.lib_SR(srNDX).Name
                                Dim isAlreadySR As Boolean = False

                                If T.numMatchingLabels(C.Labels, T.lib_SR(srNDX).Labels) / C.numLabels > 0.9 Then
                                    lName += " [ALREADY APPLIED]"
                                    isAlreadySR = True
                                End If
                                Console.WriteLine("       SR : [" + T.lib_SR(srNDX).Id.ToString + "]" + spaces(10 - Len(T.lib_SR(srNDX).Id.ToString)) + " " + lName)

                                If isAlreadySR Then GoTo escapeHere
showEditSRAgain2:
                                Console.WriteLine("Keep this security requirement on this component? (y/n/desc)")
                                Dim keepThreat As Boolean = True
                                Dim result = Console.ReadKey()

                                Console.SetCursorPosition(0, Console.CursorTop - 1)
                                Console.WriteLine(spaces(80))
                                Console.SetCursorPosition(0, Console.CursorTop - 1)

                                If LCase(result.KeyChar.ToString) = "d" Then
                                    Console.WriteLine(T.lib_SR(srNDX).Description)
                                    GoTo showEditSRAgain2
                                End If

                                If LCase(result.KeyChar.ToString) = "y" Then
                                    If T.matchLabelsOnSR(SR, C.Name) Then
                                        Call T.addEditSR(SR)
                                    Else
                                        Console.WriteLine("Could not match labels on " + SR.Id.ToString + " to component " + C.Id.ToString)
                                    End If
                                End If

                                If LCase(result.KeyChar.ToString) = "n" Then
                                    Dim isDUP As Boolean = False
                                    If isDUP = True Then
                                        Console.WriteLine("** This SR is linked to multiple threats of this Component **")
                                        Console.WriteLine("Press 'O' to OVERRIDE & remove this SR from all of this component's threats..")
                                        result = Console.ReadKey()
                                        Console.SetCursorPosition(0, Console.CursorTop - 2)
                                        Console.WriteLine(spaces(120))
                                        Console.WriteLine(spaces(120))
                                        Console.SetCursorPosition(0, Console.CursorTop - 2)
                                        If result.KeyChar.ToString <> "O" Then
                                            GoTo escapeHere
                                        End If
                                    End If
                                    If T.removeLabelFromSR(SR, C.Name) Then
                                        Call T.addEditSR(SR)
                                    Else
                                        Console.WriteLine("Could not find label '" + C.Name + "' on SR")
                                    End If
                                Else
                                    'Console.WriteLine(vbCrLf + "Keeping SR on component's threat")
                                End If
escapeHere:




                            Next
                        Next
                        Console.WriteLine("========================")
                    Next
                End With

                End


            Case "get_comp"
                Dim cID As Integer = Val(argValue("id", args))
                Dim cNAME = argValue("name", args)

                Dim editSTR$ = argValue("edit", args)
                Dim doEDIT As Boolean = False
                If LCase(editSTR) = "true" Then doEDIT = True

                If cID = 0 And cNAME = "" Then
                    Console.WriteLine("Must provide either an ID or NAME argument to identify component of interest")
                    End
                End If

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)

                Dim ndxC As Integer

                Dim C As tmComponent

                If cID Then
                    ndxC = T.ndxComp(cID)
                    If ndxC = -1 Then
                        Console.WriteLine("Component does not exist")
                        End
                    End If
                    Console.WriteLine("Found Component " + cID.ToString + ": " + T.lib_Comps(ndxC).Name + " [" + T.lib_Comps(ndxC).CompID.ToString + "]")
                    C = T.lib_Comps(ndxC)
                Else
                    ndxC = T.ndxCompbyName(cNAME)
                    If ndxC = -1 Then
                        Console.WriteLine("Component does not exist")
                        End
                    End If
                    Console.WriteLine("Found Component: " + T.lib_Comps(ndxC).Name + " [" + T.lib_Comps(ndxC).CompID.ToString + "]")
                    C = T.lib_Comps(ndxC)
                End If
                Call getBuiltComponent(C, doEDIT)
                End

            Case "get_attr"
                Dim R As New tfRequest
                Dim cID As Integer = Val(argValue("id", args))
                Dim searcH$ = argValue("search", args)

                If Len(searcH) Then searcH = LCase(searcH)
                With R
                    .EntityType = "Attributes"
                    .LibraryId = 0
                    .ShowHidden = False
                End With

                T.lib_AT = T.getTFAttr(R)

                Dim numItems As Integer = 0
                Dim IDofAT As Integer = 0

                For Each P In T.lib_AT
                    If cID And P.Id <> cID Then GoTo skipATT
                    IDofAT = P.Id
                    If Len(searcH) And InStr(LCase(P.Name), searcH) = 0 Then GoTo skipATT
                    Dim idSt$ = "[" + P.Id.ToString + "]"
                    Console.WriteLine("ATTRIBUTE :" + idSt + spaces(10 - Len(idSt)) + P.Name) ' P.CreatedByName + Space(30 - Len(P.CreatedByName)), " ", "Vers " + P.Version))
                    numItems += 1
skipATT:
                Next
                If cID = 0 Then Console.WriteLine("# of items in this list: " + numItems.ToString)
                If cID And numItems = 1 Then
                    R.EntityType = "SecurityRequirements"
                    T.lib_SR = T.getTFSecReqs(R)
                    R.EntityType = "Threats"
                    T.lib_TH = T.getTFThreats(R)
                    'Console.WriteLine("Loaded SR,TH object data")

                    Console.WriteLine("========================")
                    With T.lib_AT(T.ndxATTR(IDofAT))
                        For Each O In .Options
                            Console.WriteLine("OPTION  : " + O.Name + "     #THR: " + O.Threats.Count.ToString)
                            For Each tt In O.Threats
                                Dim idSt$ = "[" + tt.Id.ToString + "]"
                                Console.WriteLine("========================")
                                Console.WriteLine("THREAT    : " + idSt + spaces(10 - Len(idSt)) + tt.Name) ' P.CreatedByName + Space(30 - Len(P.CreatedByName)), " ", "Vers " + P.Version))
                                Call T.defineTransSRs(tt.Id)
                                For Each tsR In T.lib_TH(T.ndxTHlib(tt.Id)).listLinkedSRs
                                    Dim srNDX As Integer = T.ndxSRlib(tsR)
                                    Console.WriteLine("       SR : [" + T.lib_SR(srNDX).Id.ToString + "]" + spaces(10 - Len(T.lib_SR(srNDX).Id.ToString)) + " " + T.lib_SR(srNDX).Name)

                                Next
                            Next
                            Console.WriteLine("========================")
                        Next
                    End With

                End If
                End

            Case "show_comp"
                Dim libName$ = argValue("lib", args)
                Dim typeName$ = argValue("type", args)
                Dim searchName$ = argValue("search", args)

                Dim showUsage As Boolean = False
                If argValue("showusage", args) = "true" Then showUsage = True

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)
                T.librarieS = T.getLibraries

                Dim numLISTED As Integer = 0

                If showUsage Then
                    Dim PP As List(Of tmProjInfo)
                    PP = T.getAllProjects()

                    Console.WriteLine("Evaluating component usage across " + PP.Count.ToString + " projects..")

                    loadComponentUsage(T, PP)
                End If

                Dim labelOrUsage$ = "LABELS"
                If showUsage Then labelOrUsage = spaces(20) + "# MOD   # APP"
                Console.WriteLine(col5CLI("NAME [ID]", "TYPE", "LIBRARY", "", labelOrUsage))
                Console.WriteLine(col5CLI("---------", "----", "-------", "", spaces(20) + "------"))

                Dim doCSV As Boolean
                Dim fileN$ = argValue("file", args)
                Dim FF As Integer

                If Len(fileN) Then
                    doCSV = True
                    safeKILL(fileN)
                    FF = FreeFile()
                    Console.WriteLine("Writing to CSV File: " + fileN)
                    FileOpen(FF, fileN, OpenMode.Output)
                End If

                Dim qq$ = Chr(34)

                If doCSV Then
                    Print(FF, "LIBRARY,NAME,ID,TYPE,LABELS,# MODELS,# USED" + vbCrLf) ',Username,Active,LastLogin" + vbCrLf)
                End If


                For Each C In T.lib_Comps
                    Dim libNDX As Integer = T.ndxLib(C.LibraryId)
                    Dim lName$ = ""
                    If libNDX <> -1 Then lName$ = T.librarieS(libNDX).Name
                    If libName = "" And typeName = "" And searchName = "" Then GoTo doIT

                    If Len(libName) Then
                        If LCase(lName) <> LCase(libName) Then GoTo dontDOit
                    End If

                    If Len(typeName) Then
                        If LCase(typeName) <> LCase(C.ComponentTypeName) Then GoTo dontDOit
                    End If

                    If Len(searchName) Then
                        If InStr(LCase(C.CompName), searchName) = 0 Then GoTo dontDOit
                    End If

doIT:
                    lName += " [" + C.LibraryId.ToString + "]"
                    lName += spaces(30 - Len(lName))
                    numLISTED += 1

                    Dim lastCol$ = C.Labels
                    Dim numAppearances As Integer = 0

                    If showUsage = True And C.modelsPresent.Count = 0 Then GoTo dontDOit

                    If showUsage Then
                        lastCol = C.modelsPresent.Count.ToString
                        For Each numApp In C.numInstancesPerModel
                            numAppearances += numApp
                        Next
                        lastCol += "   " + numAppearances.ToString
                    End If


                    Console.WriteLine(col5CLI(C.CompName + " [" + C.CompID.ToString + "]", C.ComponentTypeName, lName, "", lastCol))

                    If doCSV Then
                        Dim newLabels$ = C.Labels
                        newLabels += qq + "," + C.modelsPresent.Count.ToString + "," + numAppearances.ToString
                        Print(FF, qq + lName + qq + "," + qq + C.Name + qq + "," + C.Id.ToString + "," + qq + C.ComponentTypeName + qq + "," + qq + newLabels + vbCrLf)
                    End If

dontDOit:

                Next
                Console.WriteLine("# in LIST: " + numLISTED.ToString)
                If doCSV Then
                    FileClose(FF)
                End If
                End



            Case "comp_compare"
                Dim cID1 As Integer = Val(argValue("id1", args))
                Dim cID2 As Integer = Val(argValue("id2", args))
                Dim cNAME1 = argValue("name1", args)
                Dim cNAME2 = argValue("name2", args)
                Dim showSR As Boolean = True
                Dim showDIFF As Boolean = False
                Dim shareONLY As Boolean = False

                If LCase(argValue("showsr", args)) = "false" Then showSR = False
                If LCase(argValue("diffonly", args)) = "true" Then showDIFF = True
                If LCase(argValue("shareonly", args)) = "true" Then shareONLY = True

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)


                Dim C1 As tmComponent = New tmComponent
                Dim C2 As tmComponent = New tmComponent

                Dim ndxC1 As Integer = 0
                Dim ndxC2 As Integer = 0

                Console.WriteLine("Loading components to compare..")
                If Len(cNAME1) Then
                    ndxC1 = T.ndxCompbyName(cNAME1)
                    If ndxC1 = -1 Then
                        Console.WriteLine("Component " + cNAME1 + " does not exist")
                        End
                    Else
                        C1 = T.lib_Comps(ndxC1)
                    End If
                End If
                If Len(cNAME2) Then
                    ndxC2 = T.ndxCompbyName(cNAME2)
                    If ndxC2 = -1 Then
                        Console.WriteLine("Component " + cNAME2 + " does not exist")
                        End
                    Else
                        C2 = T.lib_Comps(ndxC2)
                    End If
                End If

                If cID1 Then
                    ndxC1 = T.ndxComp(cID1)
                    If ndxC1 = -1 Then
                        Console.WriteLine("Component " + cID1.ToString + " does not exist")
                        End
                    Else
                        C1 = T.lib_Comps(ndxC1)
                    End If
                End If

                If cID2 Then
                    ndxC2 = T.ndxComp(cID1)
                    If ndxC2 = -1 Then
                        Console.WriteLine("Component " + cID2.ToString + " does not exist")
                        End
                    Else
                        C2 = T.lib_Comps(ndxC2)
                    End If
                End If

                If C1.Name = "" Or C2.Name = "" Then
                    Console.WriteLine("Unable to find both components to compare.. Try again.")
                    End
                End If

                Console.WriteLine("Found Component1: " + C1.Name + " [" + C1.CompID.ToString + "]")
                Console.WriteLine("Found Component2: " + C2.Name + " [" + C2.CompID.ToString + "]")

                Call compCompare(C1, C2, showSR, showDIFF, shareONLY)
                '                Call getBuiltComponent(C, False)
                End

            Case "compattr_mappings"
                Call allCompMappings()
                End

            Case "find_dups"
                Dim ntyType$ = argValue("entity", args)
                Dim guidONLY As Boolean = False
                If LCase(argValue("guidonly", args)) = "true" Then guidONLY = True
                If ntyType = "" Then
                    Console.WriteLine("Must provide Entity Type/eg '--ENTITY Components'")
                    End
                End If

                Select Case LCase(ntyType)
                    Case "components"
                        loadNTY(T, "Components")
                        Dim aList As New Collection

                        For Each C In T.lib_Comps
                            Dim allC As List(Of tmComponent) = T.findDUPS(C, T, guidONLY)
                            If allC.Count > 1 Then
                                For Each Ca In allC
                                    Dim a$ = "[" + Ca.Id.ToString + "/" + Ca.Guid.ToString + "] " + Ca.Name
                                    If grpNDX(aList, a) = 0 Then
                                        aList.Add(a)
                                        Console.WriteLine(a)
                                    End If
                                Next
                            End If

                        Next
                    Case "attributes"
                        loadNTY(T, "Attributes")
                        Dim aList As New Collection

                        Dim sql$ = ""
                        Dim sqlTPmap$ = ""
                        For Each C In T.lib_AT
                            Dim allC As List(Of tmComponent) = T.findDUPS(C, T, guidONLY)
                            If allC.Count > 1 Then
                                For Each Ca In allC
                                    Dim nameCa$ = Ca.Name
                                    If Len(Ca.Name) > 50 Then nameCa = Mid(nameCa, 1, 48) + ".."
                                    Dim a$ = "[" + Ca.Id.ToString + "/" + Ca.Guid.ToString + "] " + nameCa
                                    Dim map = T.comp_ATTRmapping(Ca.Id)
                                    If Len(map) Then
                                        a += " COMPONENTS USING THIS: " + map
                                        'Console.WriteLine(a)
                                    End If
                                    If T.attNumThreats(C) Then
                                        a += " NUM THREATS ATTACHED: " + T.attNumThreats(C).ToString
                                    End If
                                    If grpNDX(aList, a) = 0 Then
                                        aList.Add(a)
                                        Console.WriteLine(a)
                                        If Len(map) = 0 Then
                                            sql += "'" + UCase(Ca.Guid.ToString) + "'" + ","
                                            sqlTPmap += Ca.Id.ToString + ","
                                        End If
                                    End If
                                    ' look for attached threats

                                Next
                            End If

                        Next
                        If Len(sql) Then Console.WriteLine("DELETE FROM Attributes WHERE GUID IN (" + Mid(sql, 1, Len(sql) - 1) + ")") 'sql)
                        If Len(sqlTPmap) Then Console.WriteLine("DELETE FROM PropertiesToThreatsMapping WHERE Property_Id IN (" + Mid(sqlTPmap, 1, Len(sqlTPmap) - 1) + ")") 'sql)


                    Case "securityrequirements"
                        loadNTY(T, "SecurityRequirements")
                        Dim aList As New Collection

                        For Each C In T.lib_SR
                            Dim allC As List(Of tmComponent) = T.findDUPS(C, T, guidONLY)
                            If allC.Count > 1 Then
                                For Each Ca In allC
                                    Dim a$ = "[" + Ca.Id.ToString + "/" + Ca.Guid.ToString + "] " + Ca.Name
                                    If grpNDX(aList, a) = 0 Then
                                        aList.Add(a)
                                        Console.WriteLine(a)
                                    End If
                                Next
                            End If

                        Next
                    Case "threats"
                        loadNTY(T, "Threats")
                        Dim aList As New Collection

                        For Each C In T.lib_TH
                            Dim allC As List(Of tmComponent) = T.findDUPS(C, T, guidONLY)
                            If allC.Count > 1 Then
                                For Each Ca In allC
                                    Dim a$ = "[" + Ca.Id.ToString + "/" + Ca.Guid.ToString + "] " + Ca.Name
                                    If grpNDX(aList, a) = 0 Then
                                        aList.Add(a)
                                        Console.WriteLine(a)
                                    End If
                                Next
                            End If

                        Next

                End Select
                End

            Case "i2i_threatloop_addsr"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)
                T2.lib_TH = T2.getTFThreats(R)

                R.EntityType = "SecurityRequirements"
                T.lib_SR = T.getTFSecReqs(R)
                T2.lib_SR = T2.getTFSecReqs(R)

                For Each tH In T.lib_TH
                    Call T.defineTransSRs(tH.Id)
                    Console.WriteLine("Threat ID " + tH.Id.ToString + " of i1 is Threat ID " + T2.guidTHREAT(tH.Guid.ToString).Id.ToString + " of i2 - adding " + tH.listLinkedSRs.Count.ToString + "SRs")
                    Dim destTH As tmProjThreat
                    destTH = T2.guidTHREAT(tH.Guid.ToString)

                    For Each S In tH.listLinkedSRs
                        Dim ndxSR As Integer = T.ndxSRlib(S)
                        Console.WriteLine("     ADD SR: " + T2.guidSR(T.lib_SR(ndxSR).Guid.ToString).Id.ToString)
                        Dim destSR As tmProjSecReq
                        destSR = T2.guidSR(T.lib_SR(ndxSR).Guid.ToString)
                        Call T2.addSRtoThreat(destTH, destSR.Id)
                    Next
                Next

                End

            Case "i2i_attrloop_addattr"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)
                T2.lib_TH = T2.getTFThreats(R)

                'R.EntityType = "SecurityRequirements"
                'T.lib_SR = T.getTFSecReqs(R)
                'T2.lib_SR = T2.getTFSecReqs(R)

                R.EntityType = "Attributes"
                T.lib_AT = T.getTFAttr(R)
                T2.lib_AT = T2.getTFAttr(R)


                For Each aT In T.lib_AT
                    '                    Console.WriteLine("Adding attribute " + aT.Name)     '.guidTHREAT(tH.Guid.ToString).Id.ToString +" of i2 - adding " + tH.listLinkedSRs.Count.ToString + "SRs")

                    Console.WriteLine("UPDATE Properties SET GUID='" + aT.Guid.ToString + "' WHERE Name = '" + aT.Name + "'")
                    GoTo skippingAT1


                    If aT.Options.Count <> 2 Then
                        Console.WriteLine("This attribute not compatible with API")
                        GoTo skippingAT1
                    End If

                    If aT.Options(0).Name <> "Yes" Then
                        Console.WriteLine("This attribute not compatible with API/ option 1 not YES")
                        GoTo skippingAT1
                    End If

                    Call T2.addEditATTR(aT)


                    ' With aT.Options(0)
                    ' For Each tH In .Threats
                    ' Dim destTH As tmProjThreat
                    ' If tH.Id = 1571 Then
                    ' 'eavesdropping
                    ' destTH = T2.guidTHREAT(LCase("BDA6F33A-E090-4270-9A23-4024A9DAE960"))
                    ' Else
                    ' destTH = T2.guidTHREAT(tH.Guid.ToString)
                    ' End If
                    ' Console.WriteLine("    ADD ")
                    ' Call T2.addSRtoThreat(destTH, destSR.Id)

                    '                Next
                    'End With
                    ' Next

skippingAT1:
                Next

                End




            Case "i2i_attrloop_addth"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)
                T2.lib_TH = T2.getTFThreats(R)

                'R.EntityType = "SecurityRequirements"
                'T.lib_SR = T.getTFSecReqs(R)
                'T2.lib_SR = T2.getTFSecReqs(R)

                R.EntityType = "Attributes"
                T.lib_AT = T.getTFAttr(R)
                T2.lib_AT = T2.getTFAttr(R)


                For Each aT In T.lib_AT
                    Console.WriteLine("Attribute " + aT.Name)     '.guidTHREAT(tH.Guid.ToString).Id.ToString +" of i2 - adding " + tH.listLinkedSRs.Count.ToString + "SRs")
                    '
                    If aT.Options.Count <> 2 Then
                        Console.WriteLine("This attribute not compatible with API")
                        GoTo skippingAT2
                    End If

                    If aT.Options(0).Name <> "Yes" Then
                        Console.WriteLine("This attribute not compatible with API/ option 1 not YES")
                        GoTo skippingAT2
                    End If

                    Dim destA As tmAttribute = T2.guidATTR(aT.Guid.ToString)

                    With aT.Options(0)
                        For Each tH In .Threats
                            Dim destTH As tmProjThreat

                            Dim ndxT As Integer = T.ndxTHlib(tH.Id)
                            ' have to do this because attributes dont pass the GUID of the Threat

                            Dim libT As tmProjThreat = T.lib_TH(ndxT)

                            If libT.Id = 1571 Then
                                'eavesdropping
                                destTH = T2.guidTHREAT(LCase("BDA6F33A-E090-4270-9A23-4024A9DAE960"))
                            Else
                                destTH = T2.guidTHREAT(libT.Guid.ToString)
                            End If

                            Console.WriteLine("     ADD TH: i1:" + tH.Id.ToString + "   i2:" + destTH.Id.ToString)
                            T2.addThreatToAttribute(destA, destTH.Id)
                        Next
                    End With
skippingAT2:
                Next
                End




            Case "i2i_clonecomp"
                'clone component from another instance
                'if item exists, orig is hidden
                'correlated objects (Threats, SR, ATTR) added as necessary
                'library loop added - 
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                Console.WriteLine("Loading everything from " + T.tmFQDN + " and " + i2)

                loadNTY(T, "Components")
                loadNTY(T2, "Components")
                loadNTY(T, "SecurityRequirements")
                loadNTY(T2, "SecurityRequirements")
                loadNTY(T, "Threats")
                loadNTY(T2, "Threats")
                loadNTY(T, "Attributes")
                loadNTY(T2, "Attributes")
                loadNTY(T, "ComponentTypes")
                loadNTY(T2, "ComponentTypes")

                T.librarieS = T.getLibraries
                T2.librarieS = T.getLibraries

                Dim libLoop = argValue("library", args)
                Dim compLoop As New Collection

                Dim TlibID As Integer = 0
                Dim T2libID As Integer = 0

                TlibID = T.ndxLibByName(libLoop, T.librarieS)
                T2libID = T2.ndxLibByName(libLoop, T2.librarieS)

                Dim findID As Integer

                If Len(libLoop) Then
                    If TlibID = -1 Then
                        Console.WriteLine("ERROR: Cannot find Library " + libLoop + " at " + T.tmFQDN)
                        End
                    End If
                    If T2libID = -1 Then
                        Console.WriteLine("WARNING: Library does not yet exist " + libLoop + " at " + T2.tmFQDN)
                        '                        End
                    End If
                    findID = T.librarieS(TlibID).Id

                    For Each C In T.lib_Comps
                        If C.LibraryId = findID Then
                            compLoop.Add(C.Guid.ToString)
                        End If
                    Next
                    GoTo loopAllComps
                End If

                Dim cID1 As Integer = Val(argValue("id", args))
                Dim cNAME1 = argValue("name", args)

                Dim sourceC As tmComponent = findLocalMatch(cID1, cNAME1)

                If sourceC.Id = 0 Then
                    Console.WriteLine("Cannot find the component at " + T.tmFQDN)
                    End
                End If

                Dim deeP As Boolean = False
                Dim builD As Boolean = False
                Dim reportOnly As Boolean = False

                If Len(argValue("deep", args)) Then
                    If LCase(argValue("deep", args)) = "true" Then deeP = True
                End If
                If Len(argValue("build", args)) Then
                    If LCase(argValue("build", args)) = "true" Then builD = True
                End If
                If Len(argValue("reportonly", args)) Then
                    If LCase(argValue("reportonly", args)) = "true" Then reportOnly = True
                End If


                Console.WriteLine(vbCrLf + "Beginning clone..")

                Call cloneComponent(sourceC, T2) ', deeP, builD, reportOnly)
                End
loopAllComps:

                For Each cGuid In compLoop
                    sourceC = T.guidCOMP(cGuid)
                    Console.WriteLine("Adding.. " + sourceC.Name)
                    Call cloneComponent(sourceC, T2) ', deeP, builD, reportOnly)

                Next
                End

            Case "i2i_comploop_addth"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)
                T2.lib_TH = T2.getTFThreats(R)

                R.EntityType = "Components"
                T.lib_Comps = T.getTFComponents(R)
                T2.lib_Comps = T2.getTFComponents(R)

                R.EntityType = "SecurityRequirements"
                T.lib_SR = T.getTFSecReqs(R)
                ' T2.lib_Comps = T2.getTFComponents(R)

                R.EntityType = "Attributes"
                T.lib_AT = T.getTFAttr(R)

                Console.WriteLine("Loaded everything from " + T.tmFQDN + " - copying to " + i2)

                For Each C In T.lib_Comps
                    Dim destC As tmComponent
                    destC = T2.guidCOMP(C.Guid.ToString)

                    If IsNothing(destC) = True Then
                        Console.WriteLine("ERROR Cannot find " + C.Id.ToString + " " + C.Guid.ToString)
                        GoTo skipComp1
                    End If

                    Console.WriteLine(C.Name + " [" + C.Id.ToString + "] is Comp ID " + destC.Id.ToString + " of i2") ' - adding " + C.listThreats.Count.ToString + " Threats")
                    If C.Id = 2965 Then
                        Console.WriteLine("Skipping this one")
                        GoTo skipComp1
                    End If

                    Call T.buildCompObj(C)

                    For Each tH In C.listThreats
                        Dim destTH As tmProjThreat
                        If tH.Id = 1571 Then
                            'eavesdropping
                            destTH = T2.guidTHREAT(LCase("BDA6F33A-E090-4270-9A23-4024A9DAE960"))
                        Else
                            destTH = T2.guidTHREAT(tH.Guid.ToString)
                        End If

                        If destTH.Id = 0 Then
                            Console.WriteLine("ERROR HERE.. COULD NOT MATCH THE DESTINATION THREAT TO THE SOURCE THREAT")
                            GoTo skipComp1
                        End If

                        Call T2.addThreatToComponent(destC, destTH.Id)

                        Console.WriteLine("     ADD TH: i1:" + tH.Id.ToString + "   i2:" + destTH.Id.ToString)
                        '                        Call T2.addSRtoThreat(destTH, destSR.Id)
                    Next
skipComp1:

                Next

                End


            Case "i2i_comploop_addattr"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)
                T2.lib_TH = T2.getTFThreats(R)

                R.EntityType = "Components"
                T.lib_Comps = T.getTFComponents(R)
                T2.lib_Comps = T2.getTFComponents(R)

                R.EntityType = "SecurityRequirements"
                T.lib_SR = T.getTFSecReqs(R)
                ' T2.lib_Comps = T2.getTFComponents(R)

                R.EntityType = "Attributes"
                T.lib_AT = T.getTFAttr(R)
                T2.lib_AT = T2.getTFAttr(R)

                Console.WriteLine("Loaded everything from " + T.tmFQDN + " - copying to " + i2)

                Dim numCompsWithAT As Integer = 0
                Dim firstAT As Boolean = True

                For Each C In T.lib_Comps
                    Dim destC As tmComponent
                    destC = T2.guidCOMP(C.Guid.ToString)

                    If IsNothing(destC) = True Then
                        Console.WriteLine("ERROR Cannot find " + C.Id.ToString + " " + C.Guid.ToString)
                        GoTo skipComp2
                    End If

                    If C.Id = 2965 Then
                        Console.WriteLine("Skipping this one")
                        GoTo skipComp2
                    End If

                    Console.WriteLine("Building " + C.Name + " [" + C.Id.ToString + "] is Comp ID " + destC.Id.ToString + " on i2") ' - # ATTR: " + C.listAttr.Count.ToString) ' - adding " + C.listThreats.Count.ToString + " Threats")

                    Call T.buildCompObj(C)

                    If C.listAttr.Count = 0 Then GoTo skipComp2

                    'has attributes
                    Console.WriteLine(destC.Name + " [" + destC.Id.ToString + "] # ATTR: " + C.listAttr.Count.ToString) ' - adding " + C.listThreats.Count.ToString + " Threats")

                    firstAT = True

                    For Each AT In C.listAttr

                        If AT.Options.Count <> 2 Then
                            Console.WriteLine("This attribute not compatible with API")
                            GoTo skipAddAT
                        End If

                        If AT.Options(0).Name <> "Yes" Then
                            Console.WriteLine("This attribute not compatible with API/ option 1 not YES")
                            GoTo skipAddAT
                        End If

                        Dim ndxAT As Integer = T.ndxATTR(AT.Id)

                        Dim destAT As tmAttribute

                        destAT = T2.guidATTR(T.lib_AT(ndxAT).Guid.ToString)

                        If IsNothing(destAT) Then
                            Console.WriteLine("Attribute GUID " + T.lib_AT(ndxAT).Guid.ToString + " does not exist at " + T2.tmFQDN + " (probably incompatible)")
                            GoTo skipAddAT
                        End If

                        Console.WriteLine("     ADD ATTR: i1:" + AT.Id.ToString + "   i2:" + destAT.Id.ToString)
                        Call T2.addAttributeToComponent(destC, destAT.Id)
                        If firstAT = True Then
                            numCompsWithAT += 1
                            firstAT = False
                        End If
skipAddAT:
                    Next


skipComp2:

                Next

                Console.WriteLine("# of Components with Attributes: " + numCompsWithAT.ToString)
                End


            Case "sql_clean_roles_elements_widgets"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                'Dim T2 As TM_Client = returnTMClient(i2)
                'If IsNothing(T2) = True Then
                '    Console.WriteLine("Unable to connect to " + i2)
                '    End
                'End If

                'If T2.isConnected = True Then
                '    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                'Else
                '    Console.WriteLine("Unable to connect to " + i2)
                '    End
                'End If
                Dim R As tfRequest
                R = New tfRequest

                Dim c$ = Chr(34)

                R.EntityType = "DataElements"
                Dim TFdataElements As New List(Of tmMiscTrigger)
                TFdataElements = T.getEntityMisc(R)
                R.EntityType = "Roles"
                Dim TFroles As New List(Of tmMiscTrigger)
                TFroles = T.getEntityMisc(R)
                R.EntityType = "Widgets"
                Dim TFwidgets As New List(Of tmMiscTrigger)
                TFwidgets = T.getEntityMisc(R)

                Dim a$ = ""
                Dim sqlDataElements$ = ""
                Dim sqlRoles$ = ""
                Dim sqlWidgets$ = ""
                Dim sqlLabels$ = ""

                For Each D In TFdataElements
                    '                   Dim TH As List(Of tmProjThreat) = T.threatsOfEntity(D, "DataElements")
                    a$ += "'" + D.Name + "',"
                Next
                sqlDataElements = "DELETE FROM DataElements WHERE Name NOT IN (" + Mid(a, 1, Len(a) - 1) + ")"
                sqlLabels += a

                a$ = ""
                For Each tfR In TFroles
                    'Dim TH As List(Of tmProjThreat) = T.threatsOfEntity(tfR, "Roles")
                    a$ += "'" + tfR.Name + "',"
                    '  + TH.Count.ToString + ",1"
                    'Console.WriteLine(a$)
                    'If FF Then PrintLine(FF, a)
                Next
                sqlRoles = "DELETE FROM Roles WHERE Name NOT IN (" + Mid(a, 1, Len(a) - 1) + ")"
                sqlLabels += a


                a = ""
                For Each W In TFwidgets
                    Dim widgetThreats As List(Of tmBackendThreats) = T.threatsByBackend(W)

                    a$ += "'" + W.Name + "',"
                    sqlLabels += "'" + W.Name + "',"

                    Dim backendNames As New Collection

                    For Each B In widgetThreats
                        If grpNDX(backendNames, B.BackendName) = 0 Then
                            '                            a$ = "Widget," + W.Name + "," + B.BackendName + "," + numWidgetThreats(widgetThreats, B.BackendId).ToString + ",1"
                            '                           Console.WriteLine(a$)
                            '                          If FF Then PrintLine(FF, a)
                            backendNames.Add(B.BackendName)
                            a$ += "'" + B.BackendName + "',"
                            sqlLabels += "'" + B.BackendName + "',"

                        End If
                    Next
                Next
                sqlWidgets = "DELETE FROM Widgets WHERE Name NOT IN (" + Mid(a, 1, Len(a) - 1) + ")"

                Console.WriteLine(vbCrLf + vbCrLf + "ROLES")
                Console.WriteLine(sqlRoles)
                Console.WriteLine(vbCrLf + vbCrLf + "DATA ELEMENTS")
                Console.WriteLine(sqlDataElements)
                Console.WriteLine(vbCrLf + vbCrLf + "WIDGETS")
                Console.WriteLine(sqlWidgets)


                ' now comps, threats, SRs, libs
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)
                'T2.lib_TH = T2.getTFThreats(R)

                R.EntityType = "Components"
                T.lib_Comps = T.getTFComponents(R)
                'T2.lib_Comps = T2.getTFComponents(R)

                R.EntityType = "SecurityRequirements"
                T.lib_SR = T.getTFSecReqs(R)
                ' T2.lib_Comps = T2.getTFComponents(R)


                T.librarieS = T.getLibraries
                '                T2.librarieS = T2.getLibraries


                Dim LL As List(Of tmLabels)
                LL = T.getLabels(False)
                Dim LM As List(Of tmLabels)
                LM = T.getLabels(True)

                Dim allLabels As New List(Of String)
                For Each L In LL
                    allLabels.Add(L.Name)
                Next
                For Each L In LM
                    allLabels.Add(L.Name)
                Next


                a = ""

                Dim newLabels As New Collection

                With T
                    For Each L In .librarieS
                        If grpNDX(newLabels, L.Name) = 0 Then
                            newLabels.Add(L.Name)
                            '                            a += "'" + L.Name + "',"
                        End If
                    Next
                End With

                For Each cP In T.lib_Comps
                    Dim cL As New Collection
                    cL = CSVtoCOLL(cP.Labels)
                    For Each newL In cL
                        If grpNDX(newLabels, newL, False) = 0 Then newLabels.Add(newL)
                    Next
                Next

                For Each sR In T.lib_SR
                    Dim cL As New Collection
                    cL = CSVtoCOLL(sR.Labels)
                    For Each newL In cL
                        If grpNDX(newLabels, newL, False) = 0 Then newLabels.Add(newL)
                    Next
                Next

                For Each tH In T.lib_TH
                    Dim cL As New Collection
                    cL = CSVtoCOLL(tH.Labels)
                    For Each newL In cL
                        If grpNDX(newLabels, newL, False) = 0 Then newLabels.Add(newL)
                    Next
                Next

                ' now figure out what should be deleted
                For Each lbL In allLabels
                    If grpNDX(newLabels, lbL) = 0 Then
                        'label exists but is not part of required labels (threats, srs, etc)
                        a += "'" + lbL + "',"
                    End If
                Next


                sqlLabels = "DELETE FROM Labels WHERE Name IN (" + Mid(a, 1, Len(a) - 1) + ")"
                Console.WriteLine(vbCrLf + vbCrLf + "LABELS")
                Console.WriteLine(sqlLabels)

'                With R
'                    .EntityType = "Components"
'                    .LibraryId = 0
'                    .ShowHidden = False
'                End With
'                T.lib_Comps = T.getTFComponents(R)


            Case "i2i_lib_compare"
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                T.librarieS = T.getLibraries
                T2.librarieS = T2.getLibraries

                Console.WriteLine(vbCrLf + T.tmFQDN + " LIBRARIES:")
                Console.WriteLine("==============================")

                With T
                    For Each L In .librarieS
                        Console.WriteLine(L.Name + " [" + L.Id.ToString + "] " + L.Guid.ToString)
                    Next
                End With

                Console.WriteLine(T2.tmFQDN + " LIBRARIES:")
                Console.WriteLine("==============================")

                With T2
                    For Each L In .librarieS
                        Console.WriteLine(L.Name + " [" + L.Id.ToString + "] " + L.Guid.ToString)
                    Next
                End With

                End

            Case "i2i_sql_libs"
                ' this is useless!
                Dim i2 = argValue("i2", args)

                Dim entC As New Collection
                entC.Add("Components")
                entC.Add("SecurityRequirements")
                entC.Add("Threats")
                entC.Add("Properties")
                entC.Add("DataElements")
                entC.Add("Roles")
                entC.Add("Widgets")


                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                T.librarieS = T.getLibraries
                T2.librarieS = T2.getLibraries

                Console.WriteLine(T.tmFQDN + " LIBRARIES:")
                Console.WriteLine("==============================")

                Dim libs2Update As New Collection
                Dim blockerS As New Collection

                For Each L In T.librarieS
                    Dim ndxL As Integer = 0
                    ndxL = T2.ndxLibByName(L.Name, T2.librarieS)

                    If ndxL = -1 Then
                        blockerS.Add("You need library: " + L.Name + " [" + L.Id.ToString + "] " + L.Guid.ToString)
                    Else
                        If L.Guid.ToString <> T2.librarieS(ndxL).Guid.ToString Then
                            blockerS.Add("You need to align GUID for " + L.Name + " [" + L.Id.ToString + "] " + L.Guid.ToString)
                        Else
                            ' libs exist in both instances, issue update for new ID
                            If L.Id <> T2.librarieS(ndxL).Id Then
                                For Each ET In entC
                                    libs2Update.Add("UPDATE " + ET + " SET LibraryId=" + T2.librarieS(ndxL).Id.ToString + " WHERE LibraryId=" + L.Id.ToString)
                                Next
                            End If
                        End If
                    End If
                Next

                If blockerS.Count Then
                    For Each B In blockerS
                        Console.WriteLine(B)
                    Next
                    Console.WriteLine(vbCrLf + "CANNOT CONTINUE - Please fix libraries")
                End If
                For Each Sq In libs2Update
                    Console.WriteLine(Sq)
                Next

                End

            Case "template_convert"

                Dim i2 = argValue("i2", args)
                Dim tsID = Val(argValue("id", args))
                Dim showMatch As Boolean = False
                Dim newSQL As Boolean = False
                If LCase(argValue("showmatch", args)) = "true" Then showMatch = True
                If LCase(argValue("sql", args)) = "true" Then newSQL = True

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " And " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                Dim listT As List(Of tmTemplate) = T2.getTemplates

goAgain:

                Console.WriteLine("# of Templates: " + listT.Count.ToString + vbCrLf + vbCrLf)

                loadNTY(T, "Components")
                loadNTY(T2, "Components")

                Dim sqL$ = ""

                For Each tS In listT
                    If tsID Then
                        If tS.Id <> tsID Then GoTo nextTMP
                    End If
                    Console.WriteLine(vbCrLf + vbCrLf + "Template: " + tS.Id.ToString + ": " + tS.Name + "    LibraryId: " + tS.LibraryId.ToString)
                    Console.WriteLine("------------------------------------------")

                    sqL = tS.Json

                    Dim qq$ = Chr(34)

                    Dim nIds As List(Of String) = jsonValues(tS.Json, "NodeId")
                    Dim ndxNID As Integer = 0

                    For Each N In nIds
                        Dim C1 As tmComponent
                        Dim C2 As tmComponent

                        Dim ndxC1 As Integer = 0
                        ndxC1 = T.ndxComp(Val(N))

                        If ndxC1 = -1 Then
                            Console.WriteLine("Unable to find NodeId=" + N + " (NAME=" + jsonGetNear(tS.Json, "NodeId" + Chr(34) + ":" + N, "Name") + ")")
                            GoTo nextNID
                        End If

                        C1 = T.lib_Comps(ndxC1)

                        If ndxC1 <> -1 Then
                            C2 = T2.bestMatch(C1, False)

                            If C2.Id <> 0 Then
                                If showMatch Then Console.WriteLine("[" + C1.Id.ToString + "/" + C1.Guid.ToString + "] " + C1.Name + " DEST: [" + C2.Id.ToString + "/" + C2.Guid.ToString + "] " + C2.Name)

                                sqL = Replace(sqL, qq + "NodeId" + qq + ":" + C1.Id.ToString + ",", qq + "NodeId" + qq + ":" + C2.Id.ToString + ",")

                            Else
                                Console.WriteLine("Cannot find match for " + "[" + C1.Id.ToString + "/" + C1.Guid.ToString + "] " + C1.Name)
                            End If

                        Else
                            Console.WriteLine("Cannot find match for " + "[" + C1.Id.ToString + "/" + C1.Guid.ToString + "] " + C1.Name)
                        End If

nextNID:
                        ndxNID += 1
                    Next


                    Dim pIds As List(Of String) = jsonValues(tS.Json, "protocolIds")

                    For Each N In pIds
                        Dim C1 As tmComponent
                        Dim C2 As tmComponent

                        Dim origStr$ = qq + "protocolIds" + qq + ":" + N + "]"

                        N = Trim(Replace(N, "[", ""))
                        N = Replace(N, "]", "")

                        Dim ndxC1 As Integer = 0

                        Dim allProts As New Collection
                        allProts = CSVtoCOLL(N)

                        Dim newStr$ = ""
                        For Each nuM In allProts
                            ndxC1 = T.ndxComp(Val(nuM))

                            If ndxC1 = -1 Then
                                Console.WriteLine("Unable to find ProtocolId=" + nuM)
                                GoTo nextPID
                            End If

                            C1 = T.lib_Comps(ndxC1)

                            If ndxC1 <> -1 Then
                                C2 = T2.bestMatch(C1, False)

                                If C2.Id <> 0 Then
                                    'Console.WriteLine("[" + C1.Id.ToString + "/" + C1.Guid.ToString + "] " + C1.Name + " DEST: [" + C2.Id.ToString + "/" + C2.Guid.ToString + "] " + C2.Name)
                                    newStr += C2.Id.ToString + ","
                                Else
                                    Console.WriteLine("Cannot find match for " + "[" + C1.Id.ToString + "/" + C1.Guid.ToString + "] " + C1.Name)
                                End If

                            Else
                                Console.WriteLine("Cannot find match for " + "[" + C1.Id.ToString + "/" + C1.Guid.ToString + "] " + C1.Name)
                            End If
                        Next
                        newStr = qq + "protocolIds" + qq + ":[ " + Mid(newStr, 1, Len(newStr) - 1) + " ]"
                        If showMatch Then Console.WriteLine(origStr + "      ---> " + newStr)
                        sqL = Replace(sqL, origStr, newStr)
nextPID:
                    Next


                    If newSQL Then Console.WriteLine("UPDATE Templates SET Json='" + sqL + "' WHERE Id=" + tS.Id.ToString)
nextTMP:

                Next

                End


            Case "i2i_bestmatch"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " And " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                Dim ntyType$ = argValue("entity", args)
                If ntyType = "" Then
                    Console.WriteLine("Must provide Entity Type/eg '--ENTITY Components'")
                    End
                End If

                Dim cID1 As Integer = Val(argValue("id", args))
                Dim cNAME1 = argValue("name", args)


                loadNTY(T, ntyType)
                loadNTY(T2, ntyType)

                Dim sourceO As Object
                Dim ndX As Integer = 0

                Select Case ntyType
                    Case "Components"
                        If Len(cNAME1) Then
                            ndX = T.ndxCompbyName(cNAME1)
                            Console.WriteLine("Looking for '" + cNAME1 + "' inside " + T.tmFQDN)
                            If ndX = -1 Then
                                Console.WriteLine("Component " + cNAME1 + " does not exist")
                                End
                            Else
                                sourceO = T.lib_Comps(ndX)
                            End If
                        End If
                        If cID1 Then
                            ndX = T.ndxComp(cID1)
                            If ndX = -1 Then
                                Console.WriteLine("Component " + cID1.ToString + " does Not exist")
                                End
                            Else
                                sourceO = T.lib_Comps(ndX)

                            End If
                        End If

                    Case "Threats"
                        If Len(cNAME1) Then
                            ndX = T.ndxTHbyName(cNAME1)
                            Console.WriteLine("Looking for '" + cNAME1 + "' inside " + T.tmFQDN)
                            If ndX = -1 Then
                                Console.WriteLine("Threat " + cNAME1 + " does not exist")
                                End
                            Else
                                sourceO = T.lib_TH(ndX)
                            End If
                        End If
                        If cID1 Then
                            ndX = T.ndxTHlib(cID1)
                            If ndX = -1 Then
                                Console.WriteLine("Threat " + cID1.ToString + " does Not exist")
                                End
                            Else
                                sourceO = T.lib_TH(ndX)

                            End If
                        End If
                    Case "SecurityRequirements"
                        If Len(cNAME1) Then

                            ndX = T.ndxSRbyName(cNAME1)
                            Console.WriteLine("Looking for '" + cNAME1 + "' inside " + T.tmFQDN)
                            If ndX = -1 Then
                                Console.WriteLine("SR " + cNAME1 + " does not exist")
                                End
                            Else
                                sourceO = T.lib_SR(ndX)
                            End If
                        End If

                        If cID1 Then
                            ndX = T.ndxSRlib(cID1)
                            If ndX = -1 Then
                                Console.WriteLine("SR " + cID1.ToString + " does Not exist")
                                End
                            Else
                                sourceO = T.lib_SR(ndX)

                            End If
                        End If

                    Case "Attributes"
                        If Len(cNAME1) Then

                            ndX = T.ndxATTRbyName(cNAME1)
                            Console.WriteLine("Looking for '" + cNAME1 + "' inside " + T.tmFQDN)
                            If ndX = -1 Then
                                Console.WriteLine("Attribute " + cNAME1 + " does not exist")
                                End
                            Else
                                sourceO = T.lib_AT(ndX)
                            End If
                        End If

                        If cID1 Then
                            ndX = T.ndxATTR(cID1)
                            If ndX = -1 Then
                                Console.WriteLine("Attribute " + cID1.ToString + " does Not exist")
                                End
                            Else
                                sourceO = T.lib_TH(ndX)

                            End If
                        End If


                End Select

                If IsNothing(sourceO) = True Then
                    Console.WriteLine("Unable to match component")
                    End
                Else
                    If sourceO.id = 0 Then
                        Console.WriteLine("Unable to match component")
                        End
                    End If
                End If


                Dim destO As Object = T2.bestMatch(sourceO) ', ntyType)

                Call writeObj(destO, T2)
                End


            Case "i2i_allcomp_compare"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " And " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If


                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)
                T2.lib_Comps = T2.getTFComponents(R)
                T.librarieS = T.getLibraries
                T2.librarieS = T2.getLibraries

                Call loadNTY(T, "componenttypes")
                Call loadNTY(T2, "componenttypes")


                Dim numItems As Integer = 0
                Dim numDiffs As Integer = 0

                Dim diffS(10) As Collection
                diffS(0) = New Collection
                diffS(1) = New Collection
                diffS(2) = New Collection
                diffS(3) = New Collection
                diffS(4) = New Collection
                diffS(5) = New Collection
                diffS(6) = New Collection
                diffS(7) = New Collection
                diffS(8) = New Collection
                diffS(9) = New Collection

                Dim diffTypes As New Collection
                With diffTypes
                    .Add("GUID matches, NAME does not")
                    .Add("Item does not exist by name or GUID")
                    .Add("ID does not match (templates/models)")
                    .Add("Name matches, GUID does not") 'not used
                    .Add("Library does not match")
                    .Add("Labels do not match")
                    .Add("ComponentType does not match")
                    .Add("NAME exists with different GUID")
                    .Add("Library does not exist") 'not used
                    .Add("Non-Corp Components on dest only") 'not used
                End With

                Dim componentsFound As New Collection
                For Each cIDs In T2.lib_Comps
                    componentsFound.Add(cIDs.Id)
                Next

                Dim items2compare As New Collection

                For Each C In T.lib_Comps
                    numItems += 1
                    Dim ndxT2 As Integer = T2.ndxComp(C.Id)
                    Dim ndxT2name As Integer = T2.ndxCompbyName(C.Name)
                    Dim ndxGUID As Integer = T2.ndxCompbyGUID(C.Guid.ToString)

                    If ndxT2 + ndxT2name + ndxGUID = -3 Then
                        diffS(1).Add("[" + C.Id.ToString + "] " + C.Name)
                        GoTo nextItem
                    End If

                    Dim bestNDX As Integer = -1

                    If ndxGUID <> -1 Then
                        'GUID matches.. is it the same component?
                        bestNDX = ndxGUID
                        If C.Name <> T2.lib_Comps(ndxGUID).Name Then
                            diffS(0).Add("[" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.Name + " DEST: [" + T2.lib_Comps(bestNDX).Id.ToString + "/" + T2.lib_Comps(bestNDX).Guid.ToString + "] " + T2.lib_Comps(bestNDX).Name)
                            'GoTo nextItem
                        End If
                        'If C.Id <> T2.lib_Comps(ndxGUID).Id Then
                        '    diffS(2).Add("[" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.Name + " DEST: [" + T2.lib_Comps(bestNDX).Id.ToString + "/" + T2.lib_Comps(bestNDX).Guid.ToString + "] " + T2.lib_Comps(bestNDX).Name)
                        'End If
                    Else
                        'GUID doesnt match, can we find it by name?
                        If ndxT2name <> -1 Then
                            bestNDX = ndxT2name
                            diffS(7).Add("[" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.Name + " DEST: [" + T2.lib_Comps(bestNDX).Id.ToString + "/" + T2.lib_Comps(bestNDX).Guid.ToString + "] " + T2.lib_Comps(bestNDX).Name)
                        Else
                            diffS(1).Add("[" + C.Id.ToString + "] " + C.Name)
                            GoTo nextItem
                        End If
                    End If

                    Dim C2 As tmComponent = T2.lib_Comps(bestNDX)

                    items2compare.Add(C.Id)

                    If C2.Id <> C.Id Then
                        diffS(2).Add("[" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.Name + " DEST: [" + T2.lib_Comps(bestNDX).Id.ToString + "/" + T2.lib_Comps(bestNDX).Guid.ToString + "] " + T2.lib_Comps(bestNDX).Name)
                    End If

                    Dim removeNDX As Integer = grpNDX(componentsFound, T2.lib_Comps(bestNDX).Id)
                    If removeNDX Then
                        componentsFound.Remove(removeNDX)
                    End If


                    If C.Name <> C2.Name Then
                        'diffS.Add("NAME '" + C.Name + "' | '" + C2.Name + "'")
                    End If

                    Dim lName$ = T.librarieS(T.ndxLib(C.LibraryId)).Name
                    Dim rName$ = ""

                    If T2.ndxLib(C2.LibraryId) = -1 Then
                        diffS(8).Add("[" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.Name + " DEST: [" + C2.Id.ToString + "/" + C2.Guid.ToString + "] " + C2.Name)
                    Else
                        rName = T2.librarieS(T2.ndxLib(C2.LibraryId)).Name
                    End If

                    ' check library
                    If lName <> rName Then
                        diffS(4).Add("[" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.Name + " DEST: [" + C2.Id.ToString + "/" + C2.Guid.ToString + "] " + C2.Name)
                    End If
skipLibraryCheck:

                    ' check library
                    If C.Guid.ToString <> C2.Guid.ToString Then
                        'diffS.Add("GUID'" + C.Guid.ToString + "' | '" + C2.Guid.ToString + "'")
                    End If


                    ' labels
                    If C.Labels <> C2.Labels Then
                        diffS(5).Add("[" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.Name + " DEST: [" + C2.Id.ToString + "/" + C2.Guid.ToString + "] " + C2.Name)
                    End If

                    Dim ct2Name$ = ""
                    Dim ctName$ = ""

                    With C
                        If IsNothing(.ComponentTypeName) = True Then
                            ctName = "NULL"
                        Else
                            ctName = .ComponentTypeName
                        End If
                    End With
                    With C2
                        If IsNothing(.ComponentTypeName) = True Then
                            ct2Name = "NULL"
                        Else
                            ct2Name = .ComponentTypeName
                        End If
                    End With

                    If ctName <> ct2Name Then
                        diffS(6).Add("[" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.Name + " DEST: [" + C2.Id.ToString + "/" + C2.Guid.ToString + "] " + C2.Name)
                    End If
nextItem:
                Next

                '                Dim a$ = C.CompName + " [" + C.CompID.ToString + "]: " + spaces(50 - Len(C.CompName))

                'unique to T2
                If componentsFound.Count Then
                    For Each cIDs In componentsFound
                        Dim ndxT As Integer = T2.ndxComp(cIDs)
                        If T2.lib_Comps(ndxT).LibraryId <> 10 Then diffS(9).Add("[" + T2.lib_Comps(ndxT).Id.ToString + "] " + T2.lib_Comps(ndxT).Name)
                    Next
                End If


                Dim haveDIFFs As Integer = 0
                Dim ndxD As Integer = 0
                For ndxD = 0 To 8 ' dd In diffS
                    haveDIFFs += diffS(ndxD).Count
                Next

                Dim fileN$ = ""
                fileN = argValue("file", args)


                Dim FF As Integer = FreeFile()
                If Len(fileN) Then
                    safeKILL(fileN)
                    FileOpen(FF, fileN, OpenMode.Output)
                End If

                Dim a$ = ""
                Dim ndxI As Integer = 0
                Dim sep$ = "======================================="
                ndxD = 0
                For Each D In diffTypes
                    ndxD += 1
                    If diffS(ndxD - 1).Count Then
                        a$ = D + spaces(40 - Len(D)) + ": " + diffS(ndxD - 1).Count.ToString
                        'Console.WriteLine(vbCrLf + vbCrLf + sep)
                        If Len(fileN) Then PrintLine(FF, vbCrLf + vbCrLf + sep)
                        Console.WriteLine(a$)
                        If Len(fileN) Then PrintLine(FF, a$)
                        For ndxI = 1 To diffS(ndxD - 1).Count
                            '     Console.WriteLine(diffS(ndxD - 1).Item(ndxI))
                            If Len(fileN) Then PrintLine(FF, diffS(ndxD - 1).Item(ndxI))
                        Next
                    End If
                Next

                If Len(fileN) Then FileClose(FF)
                Console.WriteLine("=======================================")

                Console.WriteLine("# of Items: " + numItems.ToString)
                Console.WriteLine("# of Diffs: " + haveDIFFs.ToString)



                Console.WriteLine("Loading more entities for deep comparison")
                ' comp compare now
                loadNTY(T, "SecurityRequirements")
                loadNTY(T2, "SecurityRequirements")
                loadNTY(T, "Threats")
                loadNTY(T2, "Threats")
                loadNTY(T, "Attributes")
                loadNTY(T2, "Attributes")

                Console.WriteLine("Showing only components with differences" + vbCrLf)
                Console.WriteLine("=======================================")

                If Len(fileN) Then
                    FileOpen(FF, fileN, OpenMode.Append)
                End If

                Dim numStructureDiffs As Integer = 0

                For Each i In items2compare
                    Dim C1 As New tmComponent
                    Dim C2 As New tmComponent

                    C1 = T.lib_Comps(T.ndxComp(i))
                    C2 = T2.bestMatch(C1, False)

                    'If grpNDX(componentsFound, C2.Id) Then GoTo nextItem2C

                    Dim diffLines As New Collection
                    diffLines = i2i_compCompare(T2, C1, C2, True, True)


                    If diffLines.Count Then
                        numStructureDiffs += 1
                        For Each L In diffLines
                            Console.WriteLine(L)

                            If Len(fileN) Then
                                PrintLine(FF, L)
                            End If
                        Next
                    End If
nextItem2C:

                Next

                Dim clos$ = "# of Components with discrepancies: " + numStructureDiffs.ToString

                Console.WriteLine(vbCrLf + vbCrLf + clos)
                If Len(fileN) Then PrintLine(FF, vbCrLf + vbCrLf + clos)

                If Len(fileN) Then FileClose(FF)
                End



            Case "i2i_allsr_compare"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "SecurityRequirements"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_SR = T.getTFSecReqs(R)
                T2.lib_SR = T2.getTFSecReqs(R)

                Dim numItems As Integer = 0
                Dim numDiffs As Integer = 0

                For Each C In T.lib_SR
                    Dim diffS As New Collection
                    numItems += 1
                    Dim ndxT2 As Integer = T2.ndxSRlib(C.Id)

                    If ndxT2 = -1 Then
                        ' cannot find by ID
                        If T2.ndxSRbyName(C.Name) = -1 Then
                            diffS.Add("Item does not exist in I2")
                        Else
                            ndxT2 = T2.ndxSRbyName(C.Name)
                            diffS.Add("ID")
                        End If
                    End If
                    If ndxT2 = -1 Then GoTo nextItem2

                    Dim C2 As tmProjSecReq = T2.lib_SR(ndxT2)

                    ' check library
                    If C.LibraryId <> C2.LibraryId Then
                        diffS.Add("LIBRARY '" + C.LibraryId.ToString + "' | '" + C2.LibraryId.ToString + "'")
                    End If

                    ' check library
                    If C.Guid.ToString <> C2.Guid.ToString Then
                        diffS.Add("GUID'" + C.Guid.ToString + "' | '" + C2.Guid.ToString + "'")
                    End If


                    ' labels
                    If C.Labels <> C2.Labels Then
                        diffS.Add("LABELS") '" + ctName + "' | '" + ct2Name + "'")
                    End If

nextItem2:

                    Dim a$ = C.Name + " [" + C.Id.ToString + "]: " + spaces(50 - Len(C.Name))

                    If diffS.Count Then
                        numDiffs += 1
                        Dim b$ = ""
                        For Each D In diffS
                            b$ += D + ","
                        Next
                        a$ += diffS.Count.ToString + " - " + b
                    Else
                        a$ += "FULL MATCH"
                    End If

                    If InStr(a, "FULL MATCH") = 0 Then Console.WriteLine(a$)
                Next

                Console.WriteLine("# of Items: " + numItems.ToString)
                Console.WriteLine("# of Diffs: " + numDiffs.ToString)

                End


            Case "i2i_add_comps"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)
                'T2.lib_Comps = T2.getTFComponents(R)

                For Each C In T.lib_Comps
                    Dim a$ = ""
                    Console.WriteLine(C.CompName + " [" + C.CompID.ToString + "] - ADDING: " + T2.addEditCOMP(C).ToString)
                Next
                End

            Case "i2i_add_srs"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "SecurityRequirements"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_SR = T.getTFSecReqs(R)
                'T2.lib_Comps = T2.getTFComponents(R)

                For Each C In T.lib_SR
                    Dim a$ = ""
                    Console.WriteLine(C.Name + " [" + C.Id.ToString + "] - ADDING: " + T2.addSR(C).ToString) ': " 
                    '                    Call T2.addEditSR(C)
                Next
                End

            Case "i2i_add_threats"

                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim copyID As Integer = 0

                If Len(argValue("id", args)) Then copyID = Val(argValue("id", args))

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)
                'T2.lib_Comps = T2.getTFComponents(R)

                For Each C In T.lib_TH
                    If copyID Then
                        If copyID <> C.Id Then GoTo skipTH2
                    End If
                    Dim a$ = ""
                    Console.WriteLine(C.Name + " [" + C.Id.ToString + "] - ADDING: " + T2.addTH(C).ToString) ': " 
                    '                    Call T2.addEditSR(C)
skipTH2:
                Next
                End



            Case "i2i_allcomp_loop"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)
                T2.lib_Comps = T2.getTFComponents(R)

                Dim numItems As Integer = 0
                Dim numDiffs As Integer = 0

                For Each C In T.lib_Comps
                    Console.WriteLine("UPDATE Components SET GUID='" + C.Guid.ToString + "' WHERE NAME='" + C.Name + "' AND ComponentTypeId=" + C.ComponentTypeId.ToString)
                Next

                End

            Case "i2i_entitylib_sql"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                Console.WriteLine("Loading all entities for " + T.tmFQDN + " and " + i2)

                T.librarieS = T.getLibraries
                T2.librarieS = T2.getLibraries

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With

                If argValue("threats", args) = "true" Then

                    T.lib_TH = T.getTFThreats(R)
                    T2.lib_TH = T2.getTFThreats(R)

                    For Each tH In T2.lib_TH
                        Dim i1C As tmProjThreat = T.guidTHREAT(tH.Guid.ToString)

                        If i1C.Id = tH.Id Then GoTo skipTHLib

                        Dim i1L As Integer = T.ndxLib(i1C.LibraryId)
                        Dim i2L As Integer = T2.ndxLibByName(T.librarieS(i1L).Name, T2.librarieS)
                        Console.WriteLine("UPDATE Threats SET LibraryId=" + T2.librarieS(i2L).Id.ToString + " WHERE GUID='" + tH.Guid.ToString + "'")
skipTHLib:
                    Next

                End If

                If argValue("comps", args) = "true" Then

                    R.EntityType = "Components"
                    T.lib_Comps = T.getTFComponents(R)
                    T2.lib_Comps = T2.getTFComponents(R)

                    For Each C In T2.lib_Comps
                        Dim i1C As tmComponent = T.guidCOMP(C.Guid.ToString)

                        If i1C.Id = C.Id Then GoTo skipCompLib

                        Dim i1L As Integer = T.ndxLib(i1C.LibraryId)
                        Dim i2L As Integer = T2.ndxLibByName(T.librarieS(i1L).Name, T2.librarieS)
                        Console.WriteLine("UPDATE Components SET LibraryId=" + T2.librarieS(i2L).Id.ToString + " WHERE GUID='" + C.Guid.ToString + "'")
skipCompLib:
                    Next

                End If

                If argValue("srs", args) = "true" Then

                    R.EntityType = "SecurityRequirements"
                    T.lib_SR = T.getTFSecReqs(R)
                    T2.lib_SR = T2.getTFSecReqs(R)

                    For Each S In T2.lib_SR
                        Dim i1S As tmProjSecReq = T.guidSR(S.Guid.ToString)

                        If i1S.Id = S.Id Then GoTo skipSRLib

                        Dim i1L As Integer = T.ndxLib(i1S.LibraryId)
                        Dim i2L As Integer = T2.ndxLibByName(T.librarieS(i1L).Name, T2.librarieS)
                        If i2L = -1 Then GoTo skipSRLib

                        Console.WriteLine("UPDATE SecurityRequirements SET LibraryId=" + T2.librarieS(i2L).Id.ToString + " WHERE GUID='" + S.Guid.ToString + "'")
skipSRLib:
                    Next
                End If

                R.EntityType = "Attributes"
                T.lib_AT = T.getTFAttr(R)
                T2.lib_AT = T2.getTFAttr(R)

                For Each S In T2.lib_AT
                    Dim ndxT1 As Integer = 0
                    ndxT1 = T.ndxATTRbyName(S.Name)

                    Dim i1S As tmAttribute = T.lib_AT(ndxT1)

                    If i1S.Id = S.Id Then GoTo skipATLib

                    Dim i1L As Integer = T.ndxLib(i1S.LibraryId)
                    Dim i2L As Integer = T2.ndxLibByName(T.librarieS(i1L).Name, T2.librarieS)
                    If i2L = -1 Then GoTo skipATLib

                    Console.WriteLine("UPDATE Properties SET LibraryId=" + T2.librarieS(i2L).Id.ToString + " WHERE Name='" + S.Name + "'")
skipATLib:
                Next


                R.EntityType = "DataElements"
                Dim TFdataElements As New List(Of tmMiscTrigger)
                TFdataElements = T.getEntityMisc(R)

                For Each DE In TFdataElements

                    Dim i1L As Integer = T.ndxLib(DE.LibraryId)
                    Dim i2L As Integer = T2.ndxLibByName(T.librarieS(i1L).Name, T2.librarieS)
                    If i2L = -1 Then GoTo skipDELib

                    Console.WriteLine("UPDATE DataElements SET LibraryId=" + T2.librarieS(i2L).Id.ToString + " WHERE name='" + DE.Name + "'")
skipDELib:
                Next

skipDataElements:

                R.EntityType = "Roles"
                Dim TFroles As New List(Of tmMiscTrigger)
                TFroles = T.getEntityMisc(R)

                For Each RR In TFroles
                    Dim i1L As Integer = T.ndxLib(RR.LibraryId)
                    Dim i2L As Integer = T2.ndxLibByName(T.librarieS(i1L).Name, T2.librarieS)
                    If i2L = -1 Then GoTo skipRLib


                    Console.WriteLine("UPDATE Roles SET LibraryId=" + T2.librarieS(i2L).Id.ToString + " WHERE Name='" + RR.Name + "'")
skipRLib:
                Next


                R.EntityType = "Widgets"
                Dim TFwidgets As New List(Of tmMiscTrigger)
                TFwidgets = T.getEntityMisc(R)

                For Each W In TFwidgets
                    Dim i1L As Integer = T.ndxLib(W.LibraryId)
                    Dim i2L As Integer = T2.ndxLibByName(T.librarieS(i1L).Name, T2.librarieS)
                    If i2L = -1 Then GoTo skipWLib

                    Console.WriteLine("UPDATE Widgets SET LibraryId=" + T2.librarieS(i2L).Id.ToString + " WHERE Name='" + W.Name + "'")
skipWLib:
                Next





            Case "i2i_allsr_loop"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "SecurityRequirements"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_SR = T.getTFSecReqs(R)
                'T2.lib_Comps = T2.getTFComponents(R)

                Dim numItems As Integer = 0
                Dim numDiffs As Integer = 0

                For Each C In T.lib_SR
                    Console.WriteLine("UPDATE SecurityRequirements SET GUID='" + C.Guid.ToString + "' WHERE NAME='" + C.Name + "'")     ' AND ComponentTypeId=" + C.ComponentTypeId.ToString)
                Next

                End


            Case "i2i_allthreat_loop"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Threats"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_TH = T.getTFThreats(R)
                'T2.lib_Comps = T2.getTFComponents(R)

                Dim numItems As Integer = 0
                Dim numDiffs As Integer = 0

                For Each C In T.lib_TH
                    Console.WriteLine("UPDATE Threats SET GUID='" + C.Guid.ToString + "' WHERE NAME='" + C.Name + "'") ' AND ComponentTypeId=" + C.ComponentTypeId.ToString)
                Next

                End






            Case "i2i_allcomp_update"
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)
                T2.lib_Comps = T2.getTFComponents(R)

                Dim numItems As Integer = 0
                Dim numDiffs As Integer = 0

                For Each C In T.lib_Comps
                    Dim diffS As New Collection
                    numItems += 1
                    Dim ndxT2 As Integer = T2.ndxComp(C.Id)

                    If ndxT2 = -1 Then
                        ' cannot find by ID
                        If T2.ndxCompbyName(C.Name) = -1 Then
                            diffS.Add("Item does not exist in I2")
                        Else
                            ndxT2 = T2.ndxCompbyName(C.Name)
                            diffS.Add("ID")
                        End If
                    End If
                    If ndxT2 = -1 Then GoTo nextItem3

                    Dim C2 As tmComponent = T2.lib_Comps(ndxT2)

                    ' check library
                    If C.LibraryId <> C2.LibraryId Then
                        'diffS.Add("LIBRARY '" + C.LibraryId.ToString + "' | '" + C2.LibraryId.ToString + "'")
                    End If

                    ' check library
                    If C.Guid.ToString <> C2.Guid.ToString Then
                        diffS.Add("GUID'" + C.Guid.ToString + "' | '" + C2.Guid.ToString + "'")
                    End If


                    ' labels
                    If C.Labels <> C2.Labels Then
                        diffS.Add("LABELS") '" + ctName + "' | '" + ct2Name + "'")
                    End If

                    Dim ct2Name$ = ""
                    Dim ctName$ = ""

                    With C
                        If IsNothing(.ComponentTypeName) = True Then
                            ctName = "NULL"
                        Else
                            ctName = .ComponentTypeName
                        End If
                    End With
                    With C2
                        If IsNothing(.ComponentTypeName) = True Then
                            ct2Name = "NULL"
                        Else
                            ct2Name = .ComponentTypeName
                        End If
                    End With

                    If ctName <> ct2Name Then
                        ' diffS.Add("TYPE '" + ctName + "' | '" + ct2Name + "'")
                    End If
nextItem3:

                    Dim a$ = C.CompName + " [" + C.CompID.ToString + "]: " + spaces(50 - Len(C.CompName))

                    If diffS.Count Then
                        numDiffs += 1
                        Dim b$ = ""
                        For Each D In diffS
                            b$ += D + ","
                        Next
                        a$ += diffS.Count.ToString + " - " + b
                    Else
                        a$ += "FULL MATCH"
                    End If

                    If InStr(a, "FULL MATCH") = 0 Then Console.WriteLine(a$)
                Next

                Console.WriteLine("# of Items: " + numItems.ToString)
                Console.WriteLine("# of Diffs: " + numDiffs.ToString)

                End

            Case "i2i_comp_compare"
                'This should be comp_compare
                Dim cID1 As Integer = Val(argValue("id1", args))
                Dim cID2 As Integer = Val(argValue("id2", args))
                Dim cNAME1 = argValue("name1", args)
                Dim cNAME2 = argValue("name2", args)
                Dim i1 = argValue("i1", args)
                Dim i2 = argValue("i2", args)

                Dim T2 As TM_Client = returnTMClient(i2)
                If IsNothing(T2) = True Then
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If

                If T2.isConnected = True Then
                    Console.WriteLine("Connected to both " + T.tmFQDN + " and " + T2.tmFQDN)
                Else
                    Console.WriteLine("Unable to connect to " + i2)
                    End
                End If
                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)
                T2.lib_Comps = T2.getTFComponents(R)

                Dim C1 As tmComponent = New tmComponent
                Dim C2 As tmComponent = New tmComponent

                Dim ndxC1 As Integer = 0
                Dim ndxC2 As Integer = 0

                Console.WriteLine("Loading components to compare..")
                If Len(cNAME1) Then
                    ndxC1 = T.ndxCompbyName(cNAME1)
                    If ndxC1 = -1 Then
                        Console.WriteLine("Component " + cNAME1 + " does not exist")
                        End
                    Else
                        C1 = T.lib_Comps(ndxC1)

                    End If
                End If
                If Len(cNAME2) Then
                    ndxC2 = T2.ndxCompbyName(cNAME2)
                    If ndxC2 = -1 Then
                        Console.WriteLine("Component " + cNAME2 + " does Not exist")
                        End
                    Else
                        C2 = T2.lib_Comps(ndxC2)
                    End If
                End If

                If cID1 Then
                    ndxC1 = T.ndxComp(cID1)
                    If ndxC1 = -1 Then
                        Console.WriteLine("Component " + cID1.ToString + " does Not exist")
                        End
                    Else
                        C1 = T.lib_Comps(ndxC1)

                    End If
                End If

                If cID2 Then
                    ndxC2 = T2.ndxComp(cID2)
                    If ndxC2 = -1 Then
                        Console.WriteLine("Component " + cID2.ToString + " does Not exist")
                        End
                    Else
                        C2 = T2.lib_Comps(ndxC2)
                    End If
                End If

                If C1.Name = "" Or C2.Name = "" Then
                    Console.WriteLine("Unable to find both components to compare.. Try again.")
                    End
                End If


                Dim ctName$ = ""

                With C1
                    Console.WriteLine(vbCrLf + vbCrLf + "===============================")
                    Console.WriteLine("FQDN  : " + T.tmFQDN)
                    Console.WriteLine("COMP  : " + .CompName + " [" + .CompID.ToString + "]      Library " + .LibraryId.ToString)
                    Console.WriteLine("LABELS: " + .Labels)
                    Console.WriteLine("GUID  : " + .Guid.ToString)
                    If IsNothing(.ComponentTypeName) = True Then
                        ctName = "NULL"
                    Else
                        ctName = .ComponentTypeName
                    End If
                    ctName += " [" + .ComponentTypeId.ToString + "]"
                    Console.WriteLine("TYPE  : " + ctName)
                End With

                Dim ct2Name$ = ""
                With C2
                    Console.WriteLine("===============================")
                    Console.WriteLine("FQDN  : " + T2.tmFQDN)
                    Console.WriteLine("COMP  : " + .CompName + " [" + .CompID.ToString + "]      Library " + .LibraryId.ToString)
                    Console.WriteLine("LABELS: " + .Labels)
                    Console.WriteLine("GUID  : " + .Guid.ToString)
                    If IsNothing(.ComponentTypeName) = True Then
                        ct2Name = "NULL"
                    Else
                        ct2Name = .ComponentTypeName
                    End If
                    ctName += " [" + .ComponentTypeId.ToString + "]"
                    Console.WriteLine("TYPE  : " + ct2Name)
                End With
                Console.WriteLine("===============================")

                Dim compDiffs As New Collection
                compDiffs = i2i_compCompare(T2, C1, C2, True, False)

                If compDiffs.Count Then
                    For Each ccc In compDiffs
                        Console.WriteLine(ccc)
                    Next
                End If

                End

            Case "change_labels"
                loadNTY(T, "Components")
                loadNTY(T, "SecurityRequirements")
                T.librarieS = T.getLibraries()

                Dim oldLBL$ = argValue("current", args)
                Dim newLBL$ = argValue("new", args)

                If Len(oldLBL) = 0 Then
                    Console.WriteLine("Must specify label to search for using --CURRENT 'Label Text'")
                    End
                End If
                'GoTo SRsOnly
                For Each C In T.lib_Comps
                    With C
                        Dim newLabels$ = ""
                        newLabels = .Labels
                        If InStr(.Labels, oldLBL, CompareMethod.Text) Then
                            Console.WriteLine("NEW LABELS COMP  : " + .CompName + " [" + .CompID.ToString + "]      Library:" + .LibraryId.ToString + "   Labels:" + .Labels)
                            If Len(newLBL) Then
                                newLabels = Replace(newLabels, oldLBL, newLBL,,, CompareMethod.Text)
                            End If
                        End If

                        If newLabels <> .Labels Then
                            Console.WriteLine("----- OLD: " + .Labels + "   NEW: " + newLabels)
                            .Labels = newLabels
                            Console.WriteLine(.CompName + " [" + .CompID.ToString + "] -> " + newLabels + ": " + T.editCOMP(C, "TF_COMPONENT_UPDATED").ToString)
                        End If
                    End With
                Next

                'SRsOnly:

                Dim numCounted As Integer = 0

                Console.WriteLine(vbCrLf + "Security Requirements--------" + vbCrLf)
                For Each S In T.lib_SR
                    Dim editSR As Boolean = False
                    With S
                        Dim newLabels$ = ""
                        newLabels = .Labels
                        If InStr(.Labels, oldLBL, CompareMethod.Text) Then
                            Console.WriteLine("NEW LABELS SR  : " + .Name + " [" + .Id.ToString + "]      Library:" + .LibraryId.ToString + "   Labels:" + .Labels)
                            If Len(newLBL) Then
                                newLabels = Replace(newLabels, oldLBL, newLBL,,, CompareMethod.Text)
                            End If
                        End If

                        If newLabels <> .Labels Then
                            Console.WriteLine("----- OLD: " + .Labels + "   NEW: " + newLabels)
                            .Labels = newLabels
                            Console.WriteLine(.Name + " [" + .Id.ToString + "] -> " + newLabels + ": " + T.addEditSR(S)) '(C, "TF_COMPONENT_UPDATED").ToString)
                            '                            End If
                        End If
                    End With
                Next
                '                Console.WriteLine("# of Items: " + numCounted.ToString)
                End


            Case "addcomp_comp" ', "addcomp_comp"
                Dim cID1 As Integer = Val(argValue("id1", args))
                Dim cID2 As Integer = Val(argValue("id2", args))
                Dim cNAME1 = argValue("name1", args)
                Dim cNAME2 = argValue("name2", args)
                Dim showSR As Boolean = True

                If LCase(argValue("showsr", args)) = "false" Then showSR = False

                Dim R As tfRequest
                R = New tfRequest
                With R
                    .EntityType = "Components"
                    .LibraryId = 0
                    .ShowHidden = False
                End With
                T.lib_Comps = T.getTFComponents(R)


                Dim C1 As tmComponent = New tmComponent
                Dim C2 As tmComponent = New tmComponent

                Dim ndxC1 As Integer = 0
                Dim ndxC2 As Integer = 0

                Console.WriteLine("Loading components to compare..")
                If Len(cNAME1) Then
                    ndxC1 = T.ndxCompbyName(cNAME1)
                    If ndxC1 = -1 Then
                        Console.WriteLine("Component " + cNAME1 + " does not exist")
                        End
                    Else
                        C1 = T.lib_Comps(ndxC1)

                    End If
                End If
                If Len(cNAME2) Then
                    ndxC2 = T.ndxCompbyName(cNAME2)
                    If ndxC2 = -1 Then
                        Console.WriteLine("Component " + cNAME2 + " does Not exist")
                        End
                    Else
                        C2 = T.lib_Comps(ndxC2)
                    End If
                End If

                If cID1 Then
                    ndxC1 = T.ndxComp(cID1)
                    If ndxC1 = -1 Then
                        Console.WriteLine("Component " + cID1.ToString + " does Not exist")
                        End
                    Else
                        C1 = T.lib_Comps(ndxC1)

                    End If
                End If

                If cID2 Then
                    ndxC2 = T.ndxComp(cID2)
                    If ndxC2 = -1 Then
                        Console.WriteLine("Component " + cID2.ToString + " does Not exist")
                        End
                    Else
                        C2 = T.lib_Comps(ndxC2)
                    End If
                End If

                If C1.Name = "" Or C2.Name = "" Then
                    Console.WriteLine("Unable to find both components to compare.. Try again.")
                    End
                End If

                Call compAddComp(C1, C2, showSR) ', ng howSR, showDIFF, shareONLY)
                '                Call getBuiltComponent(C, False)
                End


            Case "get_libs"

                T.librarieS = T.getLibraries
                Console.WriteLine("Loaded libraries " + T.librarieS.Count.ToString)

                For Each L In T.librarieS
                    Console.WriteLine(L.Name + " [" + L.Id.ToString + "]")
                Next

            Case "base64"
                Dim fN$ = argValue("file", args)

                If Dir(argValue("file", args)) = "" Or fN = "" Then
                    Console.WriteLine("File Not found")
                    End
                End If

                Dim bigStr$ = ""
                Dim FF As Integer = FreeFile()

                FileOpen(FF, fN, OpenMode.Input)

                Do Until EOF(FF) = True
                    bigStr += LineInput(FF)
                Loop

                FileClose(FF)

                bigStr = Replace(bigStr, vbCrLf, "")

                Kill(fN)
                FF = FreeFile()
                FileOpen(FF, fN, OpenMode.Output)
                PrintLine(FF, bigStr)
                FileClose(FF)

                End

            Case "worddesc"
                Dim fN$ = argValue("file", args)

                If Dir(argValue("file", args)) = "" Or fN = "" Then
                    Console.WriteLine("File Not found")
                    End
                End If

                Dim bigStr$ = ""
                Dim FF As Integer = FreeFile()

                FileOpen(FF, fN, OpenMode.Input)

                Do Until EOF(FF) = True
                    bigStr += LineInput(FF)
                Loop

                FileClose(FF)

                fN += "_word.htm"

                Dim TFT As TF_Threat = New TF_Threat
                With TFT
                    .CodeTypeId = 1
                    .LibrayId = 66
                    .ComponentTypeId = 85
                    .ImagePath = "/ComponentImage/DefaultComponent.jpg"
                    .IsCopy = False
                    .Name = "SampleName7236"
                    .Description = bigStr
                    .Labels = "DevOps"
                    .RiskId = 1
                    .DataClassificationId = 1
                    'name, labels and description
                End With

                bigStr = JsonConvert.SerializeObject(TFT)

                FF = FreeFile()
                FileOpen(FF, fN, OpenMode.Output)
                PrintLine(FF, bigStr)
                FileClose(FF)

                End


        End Select
        Dim K As Integer
        K = 1
        Console.WriteLine("Your command Is Not recognized..You may use HELP for a list of commands.")

    End Sub

    Private Function returnTMClient(fqdN$) As TM_Client
        ' returnTMClient = New TM_Client

        fqdN = Replace(fqdN, "https://", "")

        Dim lgnS = getLogins()
        If lgnS.Count = 0 Then
            Console.WriteLine("Make sure you have a login file - the first entry will be used to log in")
            End
        End If

        Dim uN$ = ""
        Dim pW$ = ""
        Dim fqN$ = ""

        For Each F In lgnS
            If LCase(fqdN) = LCase(loginItem(F, 0)) Then
                uN$ = loginItem(F, 1)
                pW$ = loginItem(F, 2)
                fqN$ = "https://" + loginItem(F, 0) ', "")
                returnTMClient = New TM_Client(fqN, uN, pW)
                Return returnTMClient
            End If
        Next


        '''''''' Console.WriteLine(uN + "|" + pW)

        '        If T.isConnected = True Then


    End Function

    Private Sub compCompare(C1 As tmComponent, C2 As tmComponent, Optional ByVal showSR As Boolean = True, Optional ByVal showDIFF As Boolean = False, Optional ByVal shareONLY As Boolean = False, Optional ByVal alreadyLoaded As Boolean = False)
        tf_components = New List(Of tmComponent)
        For Each C In T.lib_Comps
            tf_components.Add(C)
        Next

        If showSR = False Then Console.WriteLine("Suppressing Security Requirements")
        If showDIFF = True Then Console.WriteLine("Only showing DIFFERENCES")
        If shareONLY = True Then Console.WriteLine("Only showing SHARED properties")

        Dim R As tfRequest = New tfRequest

        If alreadyLoaded = True Then GoTo skipLoad : 
        With R
            .EntityType = "SecurityRequirements"
            .LibraryId = 0
            .ShowHidden = False
        End With

        T.lib_SR = T.getTFSecReqs(R)
        Console.WriteLine("Loaded security req " + T.lib_SR.Count.ToString)

        R.EntityType = "Threats"
        T.lib_TH = T.getTFThreats(R)
        Console.WriteLine("Loaded threats " + T.lib_TH.Count.ToString)

        R.EntityType = "Attributes"
        T.lib_AT = T.getTFAttr(R)
        Console.WriteLine("Loaded attributes " + T.lib_AT.Count.ToString)
        Call T.buildCompObj(C1)
        Call T.buildCompObj(C2)

skipLoad:

        Console.WriteLine(vbCrLf + vbCrLf + "===============================")
        Console.WriteLine(C1.CompName + " [" + C1.CompID.ToString + "]      Library " + C1.LibraryId.ToString)
        Console.WriteLine("LABELS " + C1.Labels)
        Console.WriteLine("DESC   " + C1.Description)

        With C1
            Console.WriteLine("#1     " + .Name + spaces(50 - Len(.CompName)) + .listThreats.Count.ToString + " Threats, " + .listDirectSRs.Count.ToString + " Direct SRs, " + .listTransSRs.Count.ToString + " Transitive SRs" + ", " + .listAttr.Count.ToString + " Attr")
        End With
        Console.WriteLine(vbCrLf + vbCrLf + "===============================")
        Console.WriteLine(C2.CompName + " [" + C2.CompID.ToString + "]      Library " + C2.LibraryId.ToString)
        Console.WriteLine("LABELS " + C2.Labels)
        Console.WriteLine("DESC   " + C2.Description)
        With C2
            Console.WriteLine("#2     " + .Name + spaces(50 - Len(.CompName)) + .listThreats.Count.ToString + " Threats, " + .listDirectSRs.Count.ToString + " Direct SRs, " + .listTransSRs.Count.ToString + " Transitive SRs" + ", " + .listAttr.Count.ToString + " Attr")
        End With
        Console.WriteLine(vbCrLf + vbCrLf + "===============================")


        With C1

            Dim compSTR$ = ""

            If showSR = False Then GoTo skipDirectSRs
            Console.WriteLine("----DIRECT SECURITY REQS---")

            If .listDirectSRs.Count Then
                For Each DSR In .listDirectSRs
                    If T.ndxSR(DSR.Id, C2.listDirectSRs) = -1 Then
                        compSTR = "[1 ONLY]"
                    Else
                        compSTR = "[SHARED]"
                    End If
                    If showDIFF = True And compSTR = "[SHARED]" Then GoTo skip1

                    If shareONLY = True And compSTR <> "[SHARED]" Then GoTo skip1

                    Console.WriteLine("     " + compSTR + "  " + DSR.Name)
skip1:
                Next
            End If

            If shareONLY Then GoTo skipDirectSRs

            If C2.listDirectSRs.Count Then
                For Each DSR In C2.listDirectSRs
                    If T.ndxSR(DSR.Id, .listDirectSRs) = -1 Then
                        compSTR = "[2 ONLY]"
                        Console.WriteLine("     " + compSTR + "  " + DSR.Name)
                    End If
                Next
            End If

skipDirectSRs:

            Dim allC1Threats As List(Of tmProjThreat)
            Dim allC2Threats As List(Of tmProjThreat)

            allC1Threats = New List(Of tmProjThreat)
            allC2Threats = New List(Of tmProjThreat)

            Console.WriteLine("------THREATS/SRs-------")

            With allC1Threats
                For Each th In C1.listThreats
                    .Add(th)
                Next
                For Each A In C1.listAttr
                    For Each O In A.Options
                        For Each Th In O.Threats
                            .Add(Th)
                        Next
                    Next
                Next
            End With

            With allC2Threats
                For Each th In C2.listThreats
                    .Add(th)
                Next
                For Each A In C2.listAttr
                    For Each O In A.Options
                        For Each Th In O.Threats
                            .Add(Th)
                        Next
                    Next
                Next
            End With

            For Each thR In allC1Threats
                Dim matchedSRs As Collection = T.returnSRsWithLabelMatch(thR, C1)
                Dim c2matchedSRs As New Collection

                compSTR = ""
                If T.ndxTHofList(thR.Id, allC2Threats) = -1 Then
                    compSTR = "[1 ONLY]"
                Else
                    compSTR = "[SHARED]"
                    c2matchedSRs = T.returnSRsWithLabelMatch(thR, C2)
                End If

                If showDIFF = True And compSTR = "[SHARED]" Then GoTo skipSRsFromThreat
                If shareONLY = True And compSTR <> "[SHARED]" Then GoTo skipSRsFromThreat


                Console.WriteLine("THREAT NAME     [" + thR.Id.ToString + "]" + spaces(10 - Len(thR.Id.ToString)) + " " + thR.Name + " [" + matchedSRs.Count.ToString + " Of " + thR.listLinkedSRs.Count.ToString + " SRs] " + compSTR)

                If showSR = False Then GoTo skipSRsFromThreat

                For Each sS In matchedSRs 'these have been label matched as they are threats of the collection
                    'SR represents index
                    Dim isDUP As Boolean = False

                    If grpNDX(c2matchedSRs, sS) = 0 Then
                        compSTR = "[1 ONLY]"
                    Else
                        compSTR = "[SHARED]"
                    End If

                    Dim sNdx As Integer = T.ndxSRlib(sS) ' this is NDX of TMCLIENT Library
                    Dim SR As tmProjSecReq = T.lib_SR(sNdx)
                    ' If T.numMatchingLabels(.Labels, T.lib_SR(sNdx).Labels) / .numLabels > 0.9 Then
                    Dim showName$ = "[" + SR.Id.ToString + "] " + spaces(10 - Len(SR.Id.ToString)) + SR.Name

                    Console.WriteLine("            SR  " + showName + " " + compSTR)
                Next
skipSRsFromThreat:

            Next

            For Each thR In allC2Threats

                compSTR = ""
                If T.ndxTHofList(thR.Id, allC1Threats) = -1 Then
                    Dim matchedSRs As Collection = T.returnSRsWithLabelMatch(thR, C2)

                    compSTR = "[2 ONLY]"

                    If showDIFF = True And compSTR = "[SHARED]" Then GoTo skipSRsFromThreat2
                    If shareONLY = True And compSTR <> "[SHARED]" Then GoTo skipSRsFromThreat2

                    Console.WriteLine("THREAT NAME     [" + thR.Id.ToString + "]" + spaces(10 - Len(thR.Id.ToString)) + " " + thR.Name + " [" + matchedSRs.Count.ToString + " Of " + thR.listLinkedSRs.Count.ToString + " SRs] " + compSTR)

                    If showSR = False Then GoTo skipSRsFromThreat2

                    For Each sS In matchedSRs 'these have been label matched as they are threats of the collection
                        'SR represents index
                        Dim isDUP As Boolean = False

                        Dim sNdx As Integer = T.ndxSRlib(sS) ' this is NDX of TMCLIENT Library
                        Dim SR As tmProjSecReq = T.lib_SR(sNdx)
                        ' If T.numMatchingLabels(.Labels, T.lib_SR(sNdx).Labels) / .numLabels > 0.9 Then
                        Dim showName$ = "[" + SR.Id.ToString + "] " + spaces(10 - Len(SR.Id.ToString)) + SR.Name

                        Console.WriteLine("            SR  " + showName + " " + compSTR)
                    Next
skipSRsFromThreat2:
                End If
            Next

            Console.WriteLine("-------------------------------")

            If .listAttr.Count Then
                For Each AT In .listAttr

                    If T.ndxATTRofList(AT.Id, C2.listAttr) = -1 Then
                        compSTR = "[1 ONLY]"
                    Else
                        compSTR = "[SHARED]"
                    End If
                    If showDIFF = True And compSTR = "[SHARED]" Then GoTo skip3
                    If shareONLY = True And compSTR <> "[SHARED]" Then GoTo skip3

                    Console.WriteLine("ATTRIBUTE       [" + AT.Id.ToString + "] " + spaces(10 - Len(AT.Id.ToString)) + AT.Name + spaces(100 - Len(AT.Name)) + "    " + compSTR)
skip3:
                Next
            End If

            If shareONLY = True Then GoTo skipThat2

            If C2.listAttr.Count Then
                For Each AT In C2.listAttr

                    If T.ndxATTRofList(AT.Id, C1.listAttr) = -1 Then
                        compSTR = "[2 ONLY]"
                        Console.WriteLine("ATTRIBUTE       [" + AT.Id.ToString + "] " + spaces(10 - Len(AT.Id.ToString)) + AT.Name + spaces(100 - Len(AT.Name)) + compSTR)
                    End If
                Next
            End If

skipThat2:

        End With
        Console.WriteLine("==============================")



    End Sub

    Private Function i2i_compCompare(ByRef T2 As TM_Client, C1 As tmComponent, C2 As tmComponent, Optional ByVal showDIFF As Boolean = False, Optional ByVal alreadyLoaded As Boolean = False) As Collection
        tf_components = New List(Of tmComponent)
        For Each C In T.lib_Comps
            tf_components.Add(C)
        Next

        Dim numThreatsOff As Long = 0
        Dim numDirectSRsOff As Long = 0
        Dim numTransitiveSRsOff As Long = 0
        Dim numAttributesOff As Long = 0

        Dim numThreats As Long = 0
        Dim numDirectSRs As Long = 0
        Dim numTransitiveSRs As Long = 0
        Dim numAttributes As Long = 0

        i2i_compCompare = New Collection

        Dim showLines As Collection

        'If showDIFF = True Then Console.WriteLine("Only showing DIFFERENCES")

        Dim R As tfRequest = New tfRequest

        If alreadyLoaded = True Then GoTo skipLoad : 
        With R
            .EntityType = "SecurityRequirements"
            .LibraryId = 0
            .ShowHidden = False
        End With

        T.lib_SR = T.getTFSecReqs(R)
        T2.lib_SR = T.getTFSecReqs(R)
        Console.WriteLine("Loaded security req " + T.lib_SR.Count.ToString)

        R.EntityType = "Threats"
        T.lib_TH = T.getTFThreats(R)
        T2.lib_TH = T.getTFThreats(R)
        Console.WriteLine("Loaded threats " + T.lib_TH.Count.ToString)

        R.EntityType = "Attributes"
        T.lib_AT = T.getTFAttr(R)
        T2.lib_AT = T.getTFAttr(R)
        Console.WriteLine("Loaded attributes " + T.lib_AT.Count.ToString)

skipLoad:

        Call T.buildCompObj(C1)
        Call T2.buildCompObj(C2)

        showLines = New Collection
        Dim showD As Boolean = False

        showLines.Add(vbCrLf + vbCrLf + "============> " + T.tmFQDN + " <============")
        showLines.Add(C1.CompName + " [" + C1.CompID.ToString + "]      Library: " + C1.LibraryId.ToString)
        showLines.Add("LABELS " + C1.Labels)
        ' showLines.Add("DESC   " + C1.Description)

        'counting these is useless outside of a loop - added for potential future
        With C1
            numThreats += .listThreats.Count
            numDirectSRs += .listDirectSRs.Count
            numTransitiveSRs += .listTransSRs.Count
            numAttributes += .listAttr.Count
        End With
        With C2
            numThreats += .listThreats.Count
            numDirectSRs += .listDirectSRs.Count
            numTransitiveSRs += .listTransSRs.Count
            numAttributes += .listAttr.Count
        End With
        If C1.listThreats.Count <> C2.listThreats.Count Then
            numThreatsOff += 1
            showD = True
        End If
        If C1.listDirectSRs.Count <> C2.listDirectSRs.Count Then
            numDirectSRsOff += 1
            showD = True
        End If
        If C1.listTransSRs.Count <> C2.listTransSRs.Count Then
            numTransitiveSRsOff += 1
            showD = True
        End If
        If C1.listAttr.Count <> C2.listAttr.Count Then
            numAttributesOff += 1
            showD = True
        End If

        If showD Then

            With C1
            showLines.Add("#1     " + .Name + spaces(50 - Len(.CompName)) + .listThreats.Count.ToString + " Threats, " + .listDirectSRs.Count.ToString + " Direct SRs, " + .listTransSRs.Count.ToString + " Transitive SRs" + ", " + .listAttr.Count.ToString + " Attr")
        End With
        showLines.Add(vbCrLf + vbCrLf + "============> " + T2.tmFQDN + " <============")
        showLines.Add(C2.CompName + " [" + C2.CompID.ToString + "]      Library: " + C2.LibraryId.ToString)
        showLines.Add("LABELS " + C2.Labels)
        ' showLines.Add("DESC   " + C2.Description)
        With C2
            showLines.Add("#2     " + .Name + spaces(50 - Len(.CompName)) + .listThreats.Count.ToString + " Threats, " + .listDirectSRs.Count.ToString + " Direct SRs, " + .listTransSRs.Count.ToString + " Transitive SRs" + ", " + .listAttr.Count.ToString + " Attr")
        End With
        showLines.Add(vbCrLf + vbCrLf + "===============================")

        End If

        'GoTo skipThat2

        With C1

            Dim compSTR$ = ""

            showLines.Add("----DIRECT SECURITY REQS---")

            If .listDirectSRs.Count Then
                For Each DSR In .listDirectSRs
                    If T.ndxSR(DSR.Id, C2.listDirectSRs) = -1 Then
                        compSTR = "[1 ONLY]"
                    Else
                        compSTR = "[SHARED]"
                    End If
                    If showDIFF = True And compSTR = "[SHARED]" Then GoTo skip1

                    showD = True
                    showLines.Add("     " + compSTR + "  " + DSR.Name)
skip1:
                Next
            End If


            If C2.listDirectSRs.Count Then
                For Each DSR In C2.listDirectSRs
                    If T.ndxSR(DSR.Id, .listDirectSRs) = -1 Then
                        compSTR = "[2 ONLY]"
                        showD = True
                        showLines.Add("     " + compSTR + "  " + DSR.Name)
                    End If
                Next
            End If

skipDirectSRs:

            Dim allC1Threats As List(Of tmProjThreat)
            Dim allC2Threats As List(Of tmProjThreat)

            allC1Threats = New List(Of tmProjThreat)
            allC2Threats = New List(Of tmProjThreat)

            showLines.Add("------THREATS/SRs-------")

            With allC1Threats
                For Each th In C1.listThreats
                    .Add(th)
                Next
                For Each A In C1.listAttr
                    For Each O In A.Options
                        For Each Th In O.Threats
                            .Add(Th)
                        Next
                    Next
                Next
            End With

            With allC2Threats
                For Each th In C2.listThreats
                    .Add(th)
                Next
                For Each A In C2.listAttr
                    For Each O In A.Options
                        For Each Th In O.Threats
                            .Add(Th)
                        Next
                    Next
                Next
            End With

            For Each thR In allC1Threats
                Dim matchedSRs As Collection = T.returnSRsWithLabelMatch(thR, C1)
                Dim c2matchedSRs As New Collection

                compSTR = ""
                ' is there a best match for this threat?
                Dim bM As tmProjThreat = T2.bestMatch(thR)
                Dim searchID As Integer = 0

                'is it valid?
                If bM.Id = 0 Then
                    ' this threat doesn't even exist in the lib
                Else
                    searchID = bM.Id
                End If
                ' now find the ID of the target list of threats to make sure it matches bm.id
                If T.ndxTHofList(bM.Id, allC2Threats) = -1 Then
                    compSTR = "[1 ONLY]"
                Else
                    compSTR = "[SHARED]"
                    c2matchedSRs = T2.returnSRsWithLabelMatch(thR, C2)
                End If

                If showDIFF = True And compSTR = "[SHARED]" Then GoTo skipSRsFromThreat

                showD = True
                showLines.Add("THREAT NAME     [" + thR.Id.ToString + "]" + spaces(10 - Len(thR.Id.ToString)) + " " + thR.Name + " [" + matchedSRs.Count.ToString + " Of " + thR.listLinkedSRs.Count.ToString + " SRs] " + compSTR)


                For Each sS In matchedSRs 'these have been label matched as they are threats of the collection
                    'SR represents index
                    Dim isDUP As Boolean = False
                    Dim s1S As tmProjSecReq = T.lib_SR(sS)
                    Dim bMs As tmProjSecReq = T2.bestMatch(s1S, False)
                    Dim searchIDs As Integer = 0

                    'is it valid?
                    If bMs.Id = 0 Then
                        ' this threat doesn't even exist in the lib
                        bMs.Id = -1
                    Else
                        searchID = bMs.Id
                    End If
                    If grpNDX(c2matchedSRs, bMs.Id) = 0 Then
                        compSTR = "[1 ONLY]"
                    Else
                        compSTR = "[SHARED]"
                    End If

                    Dim sNdx As Integer = T.ndxSRlib(sS) ' this is NDX of TMCLIENT Library
                    Dim SR As tmProjSecReq = T.lib_SR(sNdx)
                    ' If T.numMatchingLabels(.Labels, T.lib_SR(sNdx).Labels) / .numLabels > 0.9 Then
                    Dim showName$ = "[" + SR.Id.ToString + "] " + spaces(10 - Len(SR.Id.ToString)) + SR.Name

                    If compSTR <> "[SHARED]" Then
                        showD = True
                        showLines.Add("            SR  " + showName + " " + compSTR)
                    End If
                Next
skipSRsFromThreat:

            Next

            For Each thR In allC2Threats

                compSTR = ""
                If T.ndxTHofList(thR.Id, allC1Threats) = -1 Then
                    Dim matchedSRs As Collection = T.returnSRsWithLabelMatch(thR, C2)

                    compSTR = "[2 ONLY]"

                    If showDIFF = True And compSTR = "[SHARED]" Then GoTo skipSRsFromThreat2

                    showD = True
                    showLines.Add("THREAT NAME     [" + thR.Id.ToString + "]" + spaces(10 - Len(thR.Id.ToString)) + " " + thR.Name + " [" + matchedSRs.Count.ToString + " Of " + thR.listLinkedSRs.Count.ToString + " SRs] " + compSTR)


                    For Each sS In matchedSRs 'these have been label matched as they are threats of the collection
                        'SR represents index
                        Dim isDUP As Boolean = False

                        Dim sNdx As Integer = T.ndxSRlib(sS) ' this is NDX of TMCLIENT Library
                        Dim SR As tmProjSecReq = T.lib_SR(sNdx)
                        ' If T.numMatchingLabels(.Labels, T.lib_SR(sNdx).Labels) / .numLabels > 0.9 Then
                        Dim showName$ = "[" + SR.Id.ToString + "] " + spaces(10 - Len(SR.Id.ToString)) + SR.Name

                        showD = True
                        showLines.Add("            SR  " + showName + " " + compSTR)
                    Next
skipSRsFromThreat2:
                End If
            Next

            showLines.Add("-------------------------------")

            If .listAttr.Count Then
                For Each AT In .listAttr

                    If T.ndxATTRofList(AT.Id, C2.listAttr) = -1 Then
                        compSTR = "[1 ONLY]"
                    Else
                        compSTR = "[SHARED]"
                    End If
                    If showDIFF = True And compSTR = "[SHARED]" Then GoTo skip3

                    showD = True
                    showLines.Add("ATTRIBUTE       [" + AT.Id.ToString + "] " + spaces(10 - Len(AT.Id.ToString)) + AT.Name + spaces(100 - Len(AT.Name)) + "    " + compSTR)
skip3:
                Next
            End If


            If C2.listAttr.Count Then
                For Each AT In C2.listAttr

                    If T.ndxATTRofList(AT.Id, C1.listAttr) = -1 Then
                        compSTR = "[2 ONLY]"
                        showD = True
                        showLines.Add("ATTRIBUTE       [" + AT.Id.ToString + "] " + spaces(10 - Len(AT.Id.ToString)) + AT.Name + spaces(100 - Len(AT.Name)) + compSTR)
                    End If
                Next
            End If
        End With

skipThat2:

        showLines.Add("==============================")

        If showD = False Then showLines = New Collection

        Return showLines

    End Function

    Private Sub compAddComp(C1 As tmComponent, C2 As tmComponent, Optional ByVal showSR As Boolean = True)
        tf_components = New List(Of tmComponent)
        For Each C In T.lib_Comps
            tf_components.Add(C)
        Next

        If showSR = False Then Console.WriteLine("Suppressing Security Requirements")

        Dim R As tfRequest = New tfRequest
        With R
            .EntityType = "SecurityRequirements"
            .LibraryId = 0
            .ShowHidden = False
        End With

        T.lib_SR = T.getTFSecReqs(R)
        Console.WriteLine("Loaded security req " + T.lib_SR.Count.ToString)
        R.EntityType = "Threats"
        T.lib_TH = T.getTFThreats(R)
        Console.WriteLine("Loaded threats " + T.lib_TH.Count.ToString)

        R.EntityType = "Attributes"
        T.lib_AT = T.getTFAttr(R)
        Console.WriteLine("Loaded attributes " + T.lib_AT.Count.ToString)
        Call T.buildCompObj(C1)
        Call T.buildCompObj(C2)


        Console.WriteLine(vbCrLf + vbCrLf + "======ADDING TO COMPONENT======")
        Dim sName$ = C1.CompName + " [" + C1.CompID.ToString + "] LIB: " + C1.LibraryId.ToString
        'Console.WriteLine("DESCRIPTION : " + C1.Description)

        With C1
            Console.WriteLine(sName + spaces(60 - Len(sName)) + .listThreats.Count.ToString + " Threats, " + .listDirectSRs.Count.ToString + " Direct SRs, " + .listTransSRs.Count.ToString + " Transitive SRs" + ", " + .listAttr.Count.ToString + " Attr")
        End With
        Console.WriteLine("LABELS      :" + C1.Labels)
        Console.WriteLine("============FROM===============")
        sName = C2.CompName + " [" + C2.CompID.ToString + "] LIB: " + C2.LibraryId.ToString
        'Console.WriteLine("DESCRIPTION : " + C2.Description)
        With C2
            Console.WriteLine(sName + spaces(60 - Len(sName)) + .listThreats.Count.ToString + " Threats, " + .listDirectSRs.Count.ToString + " Direct SRs, " + .listTransSRs.Count.ToString + " Transitive SRs" + ", " + .listAttr.Count.ToString + " Attr")
        End With
        Console.WriteLine("LABELS      : " + C2.Labels)
        Console.WriteLine("===============================")


        Dim changeS As New Collection


        With C2
            Dim compSTR$ = ""

            GoTo skipDirectSRs 'this part not complete yet

            If showSR = False Then GoTo skipDirectSRs
            Console.WriteLine("----DIRECT SECURITY REQS---")

            If .listDirectSRs.Count Then
                For Each DSR In .listDirectSRs
                    If T.ndxSR(DSR.Id, C1.listDirectSRs) = -1 Then
                        compSTR = "[1 ONLY]"
                    Else
                        compSTR = "[SHARED]"
                    End If

                    Console.WriteLine("     " + compSTR + "  " + DSR.Name)
skip1:
                Next
            End If


skipDirectSRs:

            Dim allC1Threats As List(Of tmProjThreat)
            Dim allC2Threats As List(Of tmProjThreat)

            allC1Threats = New List(Of tmProjThreat)
            allC2Threats = New List(Of tmProjThreat)

            Console.WriteLine("------THREATS/SRs-------")

            With allC1Threats
                For Each th In C1.listThreats
                    .Add(th)
                Next
                For Each A In C1.listAttr
                    For Each O In A.Options
                        For Each Th In O.Threats
                            .Add(Th)
                        Next
                    Next
                Next
            End With

            Dim fromATTR As Boolean = False

            With allC2Threats
                For Each th In C2.listThreats
                    .Add(th)
                Next
                For Each A In C2.listAttr
                    For Each O In A.Options
                        For Each Th In O.Threats
                            .Add(Th)
                            Th.Name = "ATTR/" + Th.Name
                            fromATTR = True
                        Next
                    Next
                Next
            End With

            If fromATTR Then
                Console.WriteLine("** THREATS THAT COME FROM AN ATTRIBUTE WILL BE IDENTIFIED ***")
            End If

            For Each thR In allC2Threats
                Dim matchedSRs As Collection = T.returnSRsWithLabelMatch(thR, C2)
                Dim c1matchedSRs As New Collection

                compSTR = ""
                If T.ndxTHofList(thR.Id, allC1Threats) = -1 Then
                    compSTR = ""
                Else
                    compSTR = "[SHARED]"
                    c1matchedSRs = T.returnSRsWithLabelMatch(thR, C2)
                End If

                Console.WriteLine("THREAT NAME     [" + thR.Id.ToString + "]" + spaces(10 - Len(thR.Id.ToString)) + " " + thR.Name + " [" + matchedSRs.Count.ToString + " Of " + thR.listLinkedSRs.Count.ToString + " SRs] " + compSTR)

                If compSTR = "" Then
                    ' here choose to add threat to C1
showEditThreatAgain:

                    Console.WriteLine("Add this threat to " + C1.Name + "? (y/n/desc)")
                    Dim keepThreat As Boolean = True
                    Dim result = Console.ReadKey()

                    Console.SetCursorPosition(0, Console.CursorTop - 1)
                    Console.WriteLine(spaces(80))
                    Console.SetCursorPosition(0, Console.CursorTop - 1)

                    If LCase(result.KeyChar.ToString) = "d" Then
                        Console.WriteLine(thR.Description)
                        GoTo showEditThreatAgain
                    End If

                    If LCase(result.KeyChar.ToString) = "y" Then
                        If Mid(thR.Name, 1, 5) = "ATTR/" Then thR.Name = Mid(thR.Name, 6) ' put the name back to its original state
                        Call addCompThreat(C1, thR, showSR)
                    End If

                End If

            Next

            Console.WriteLine("-------------------------------")


        End With


    End Sub

    Private Sub removeAllSRFromComp(C1 As tmComponent, TM As TM_Client, Optional ByVal threatsANDattrTOO As Boolean = False)
        ' Dim allC1Threats As New Collection

        Dim ndxS As Integer = 0
        Dim thSRs As New Collection
        Dim atSRs As New Collection

        For Each th In C1.listThreats
            '.Add(th)
            thSRs = TM.returnSRsWithLabelMatch(th, C1)
            For Each S In thSRs
                ndxS = TM.ndxSRlib(S)
                If TM.removeLabelFromSR(TM.lib_SR(ndxS), C1.Name) Then
                    Call T.addEditSR(TM.lib_SR(ndxS))
                    'make sure label is not there
                End If
            Next
            If threatsANDattrTOO Then TM.removeThreatFromComponent(C1, th.Id)
        Next
        For Each A In C1.listAttr
            For Each O In A.Options
                For Each Th In O.Threats
                    '.Add(Th)
                    atSRs = TM.returnSRsWithLabelMatch(Th, C1)
                    For Each S In atSRs
                        ndxS = TM.ndxSRlib(S)
                        If TM.removeLabelFromSR(TM.lib_SR(ndxS), C1.Name) Then
                            Call T.addEditSR(TM.lib_SR(ndxS))
                            'make sure label is not there
                        End If
                    Next

                Next
            Next
            If threatsANDattrTOO Then TM.removeAttributeFromComponent(C1, A.Id)
        Next

        'get transitive of all threats and remove labels from SRs
        'then remove THREAT mapping from Component
        'Remove Attributes from Component


    End Sub

    Public Function numWidgetThreats(ByRef W As List(Of tmBackendThreats), ndxBackEnd As Integer) As Integer
        numWidgetThreats = 0

        For Each backenD In W
            If backenD.BackendId = ndxBackEnd Then numWidgetThreats += 1
        Next

    End Function

    Private Sub addCompThreat(COMP As tmComponent, thR As tmProjThreat, Optional ByVal showSR As Boolean = True)
        Call T.addThreatToComponent(COMP, thR.Id)

        If showSR = False Then Exit Sub

        With COMP

            For Each SR In thR.listLinkedSRs
                'SR represents index
                Dim sNdx As Integer = T.ndxSRlib(SR)
                Dim lName$ = T.lib_SR(sNdx).Name
                Dim isAlreadySR As Boolean = False
                If T.numMatchingLabels(COMP.Labels, T.lib_SR(sNdx).Labels) / COMP.numLabels > 0.9 Then
                    lName += " [ALREADY APPLIED]"
                    isAlreadySR = True
                End If

                Console.WriteLine("            SR  [" + T.lib_SR(sNdx).Id.ToString + "] " + lName)

                If isAlreadySR Then GoTo escapehere
showEditThreatAgain:

                Console.WriteLine("Add this security requirement To the component? (y/n/desc)")
                Dim keepThreat As Boolean = True
                Dim result = Console.ReadKey()

                Console.SetCursorPosition(0, Console.CursorTop - 1)
                Console.WriteLine(spaces(80))
                Console.SetCursorPosition(0, Console.CursorTop - 1)

                If LCase(result.KeyChar.ToString) = "d" Then
                    Console.WriteLine(thR.Description)
                    GoTo showEditThreatAgain
                End If

                If LCase(result.KeyChar.ToString) = "y" Then
                    'Console.WriteLine(vbCrLf + "Adding SR To component's threat")
                    If T.matchLabelsOnSR(T.lib_SR(sNdx), COMP.Labels) Then
                        Call T.addEditSR(T.lib_SR(sNdx))

                    End If
                Else
                    If T.removeLabelFromSR(T.lib_SR(sNdx), COMP.Name) Then                    '
                        Call T.addEditSR(T.lib_SR(sNdx))
                        'make sure label is not there
                    End If
                End If
escapehere:

            Next

        End With
    End Sub

    Private Sub getBuiltComponent(ByRef COMP As tmComponent, Optional ByVal doEDITS As Boolean = False)
        '

        'T.labelS = T.getLabels()
        'Console.WriteLine("Loaded labels: " + T.labelS.Count.ToString)
        'T.sysLabels = T.getLabels(True)
        'Console.WriteLine("Loaded system labels: " + T.sysLabels.Count.ToString)
        'T.groupS = T.getGroups
        'Console.WriteLine("Loaded groups: " + T.groupS.Count.ToString)
        T.librarieS = T.getLibraries
        Console.WriteLine("Loaded libraries: " + T.librarieS.Count.ToString)

        Dim R As tfRequest
        R = New tfRequest
        With R
            .EntityType = "Components"
            .LibraryId = 0
            .ShowHidden = False
        End With

        tf_components = New List(Of tmComponent)
        For Each C In T.lib_Comps
            tf_components.Add(C)
        Next

        With R
            .EntityType = "SecurityRequirements"
            .LibraryId = 0
            .ShowHidden = False
        End With

        T.lib_SR = T.getTFSecReqs(R)
        Console.WriteLine("Loaded security req: " + T.lib_SR.Count.ToString)

        R.EntityType = "Threats"
        T.lib_TH = T.getTFThreats(R)
        Console.WriteLine("Loaded threats: " + T.lib_TH.Count.ToString)

        R.EntityType = "Attributes"
        T.lib_AT = T.getTFAttr(R)
        Console.WriteLine("Loaded attributes: " + T.lib_AT.Count.ToString)

        Call T.buildCompObj(COMP)

        Dim changeS As New Collection

        Console.WriteLine(vbCrLf + vbCrLf + "===============================")
        With COMP

            Dim lName$ = ""
            If T.ndxLib(.LibraryId) <> -1 Then
                lName = T.librarieS(T.ndxLib(.LibraryId)).Name + spaces(25 - Len(.CompName)) + "   LIB ID: " + .LibraryId.ToString
            Else
                lName = "[" + .LibraryId.ToString + "]"
            End If


            Console.WriteLine("DETAIL OF COMPONENT: " + .CompName + spaces(30 - Len(.CompName)) + "      ID: " + .CompID.ToString)
            Console.WriteLine("LIBRARY            : " + lName)
            Console.WriteLine("GUID               : " + .Guid.ToString)

            Console.WriteLine("LABELS             : " + .Labels)
            Console.WriteLine("IMAGE              : " + T.tmFQDN + .ImagePath)
            Console.WriteLine("DESCRIPTION        : " + .Description)
            Console.WriteLine("-------------------------------")
            Console.WriteLine("TRC CONSTRUCT      : " + .listThreats.Count.ToString + " Threats, " + .listDirectSRs.Count.ToString + " Direct SRs, " + .listTransSRs.Count.ToString + " Transitive SRs" + ", " + .listAttr.Count.ToString + " Attr")

            If .listDirectSRs.Count Then
                Console.WriteLine("-------------------------------")
                For Each DSR In .listDirectSRs
                    Console.WriteLine("     DIRECT SR : " + DSR.Name)
                Next
            End If

            Console.WriteLine("-------------------------------")
            For Each thR In .listThreats
                Dim matchedSRs As Collection = T.returnSRsWithLabelMatch(thR, COMP)

                Console.WriteLine("THREAT NAME    : [" + thR.Id.ToString + "]" + spaces(10 - Len(thR.Id.ToString)) + " " + thR.Name + " [" + matchedSRs.Count.ToString + " of " + thR.listLinkedSRs.Count.ToString + " SRs]")


                If doEDITS Then
showEditThreatAgain:
                    Console.WriteLine("Keep this threat? (y/n/desc)")
                    Dim keepThreat As Boolean = True
                    Dim result = Console.ReadKey()
                    Console.SetCursorPosition(0, Console.CursorTop - 1)
                    Console.WriteLine(spaces(80))
                    Console.SetCursorPosition(0, Console.CursorTop - 1)

                    If LCase(result.KeyChar.ToString) = "d" Then
                        Console.WriteLine(thR.Description)
                        GoTo showEditThreatAgain
                    End If

                    If LCase(result.KeyChar.ToString) = "n" Then
                        Console.WriteLine(vbCrLf + "Removing threat from component")
                        Call T.removeThreatFromComponent(COMP, thR.Id)
                        changeS.Add("Removed threat from Component mapping: " + thR.Name + " [" + thR.Id.ToString + "]")
                        GoTo skipSRsFromThreat
                    Else
                        'Console.WriteLine(vbCrLf + "Keeping threat on component")
                    End If
                End If

                For Each sS In matchedSRs 'these have been label matched as they are threats of the collection
                    'SR represents index
                    Dim isDUP As Boolean = False
                    Dim sNdx As Integer = T.ndxSRlib(sS) ' this is NDX of TMCLIENT Library
                    Dim SR As tmProjSecReq = T.lib_SR(sNdx)
                    ' If T.numMatchingLabels(.Labels, T.lib_SR(sNdx).Labels) / .numLabels > 0.9 Then
                    Dim showName$ = "[" + SR.Id.ToString + "] " + spaces(10 - Len(SR.Id.ToString)) + SR.Name
                    If grpNDX(COMP.duplicateSRs, SR.Id) Then
                        isDUP = True
                        showName += " [DUPLICATE]"
                    End If
                    Console.WriteLine("            SR : " + showName)
                    If doEDITS Then
showEditThreatAgain2:
                        Console.WriteLine("Keep this security requirement on this component? (y/n/desc)")
                        Dim keepThreat As Boolean = True
                        Dim result = Console.ReadKey()

                        Console.SetCursorPosition(0, Console.CursorTop - 1)
                        Console.WriteLine(spaces(80))
                        Console.SetCursorPosition(0, Console.CursorTop - 1)

                        If LCase(result.KeyChar.ToString) = "d" Then
                            Console.WriteLine(SR.Description)
                            GoTo showEditThreatAgain2
                        End If

                        If LCase(result.KeyChar.ToString) = "n" Then
                            If isDUP = True Then
                                Console.WriteLine("** This SR is linked to multiple threats of this Component **")
                                Console.WriteLine("Press 'O' to OVERRIDE & remove this SR from all of this component's threats..")
                                result = Console.ReadKey()
                                Console.SetCursorPosition(0, Console.CursorTop - 2)
                                Console.WriteLine(spaces(120))
                                Console.WriteLine(spaces(120))
                                Console.SetCursorPosition(0, Console.CursorTop - 2)
                                If result.KeyChar.ToString <> "O" Then
                                    GoTo escapeHere
                                End If
                            End If
                            Console.WriteLine(vbCrLf + "Removing SR from component's threat")
                            If T.removeLabelFromSR(SR, COMP.Name) Then
                                Call T.addEditSR(SR)
                                changeS.Add("Removed SR from Threat " + thR.Name + " [" + thR.Id.ToString + "] - '" + COMP.Name + "' label removed from SR: " + SR.Name + " [" + SR.Id.ToString + "]")

                            Else
                                Console.WriteLine("Could not find label '" + COMP.Name + "' on SR")
                            End If
                        Else
                            'Console.WriteLine(vbCrLf + "Keeping SR on component's threat")
                        End If
escapeHere:
                    End If

                    'End If
                Next
skipSRsFromThreat:

            Next


            If .listAttr.Count Then
                For Each AT In .listAttr
                    For Each O In AT.Options
                        If O.Threats.Count Then
                            Console.WriteLine("-------------------------------")
                            Console.WriteLine("ATTRIBUTE      : [" + AT.Id.ToString + "] " + spaces(10 - Len(AT.Id.ToString)) + AT.Name + spaces(100 - Len(AT.Name)) + "OPTION: " + O.Name + " DEFAULT: " + O.isDefault.ToString + " #TH: " + O.Threats.Count.ToString)

                            If doEDITS Then
                                Console.WriteLine("Keep this attribute on the component? (y/n)")
                                Dim keepThreat As Boolean = True
                                Dim result = Console.ReadKey()
                                Console.SetCursorPosition(0, Console.CursorTop - 1)
                                Console.WriteLine(spaces(80))
                                Console.SetCursorPosition(0, Console.CursorTop - 1)

                                If LCase(result.KeyChar.ToString) = "n" Then
                                    Console.WriteLine("Removing Attribute:" + AT.Name)
                                    Call T.removeAttributeFromComponent(COMP, AT.Id)
                                    changeS.Add("Removed attribute from Component '" + COMP.Name + "': " + AT.Name + " [" + AT.Id.ToString + "]")

                                    GoTo skipSRsFromThreatATTR
                                Else
                                    'Console.WriteLine(vbCrLf + "Keeping attribute")
                                End If
                            End If

                            For Each thR In O.Threats

                                Dim atThreatMatched As Collection = T.returnSRsWithLabelMatch(thR, COMP)

                                Console.WriteLine("        THREAT : [" + thR.Id.ToString + "] " + spaces(10 - Len(thR.Id.ToString)) + thR.Name + " [" + atThreatMatched.Count.ToString + " of " + thR.listLinkedSRs.Count.ToString + " SRs]")

                                ' Do we really want to modify ATTRIBUTE/THREAT mapping? 
                                '                                If doEDITS Then
                                '                                    Console.WriteLine("Keep this threat on the attribute? (y/n)")
                                '                                    Dim keepThreat As Boolean = True
                                '                                    Dim result = Console.ReadKey()
                                '                                    If LCase(result.KeyChar.ToString) = "n" Then
                                '                                        Console.WriteLine(vbCrLf + "Removing threat from attribute")
                                '                                        changeS.Add("Removed threat from Attribute mapping: " + thR.Name + " [" + thR.Id.ToString + "]")
                                '
                                '           GoTo skipSRsFromThreatATTR
                                '       Else
                                '           Console.WriteLine(vbCrLf + "Keeping threat on attribute")
                                '       End If
                                '   End If

                                For Each S In thR.listLinkedSRs
                                    Dim isDUP As Boolean = False
                                    Dim showName$ = ""
                                    Dim sNdx As Integer = T.ndxSRlib(S)
                                    If T.numMatchingLabels(.Labels, T.lib_SR(sNdx).Labels) / .numLabels > 0.9 Then
                                        showName = "[" + T.lib_SR(sNdx).Id.ToString + "] " + spaces(10 - Len(T.lib_SR(sNdx).Id.ToString)) + T.lib_SR(sNdx).Name
                                        If grpNDX(COMP.duplicateSRs, T.lib_SR(sNdx).Id) Then
                                            isDUP = True
                                            showName += " [DUPLICATE]"
                                        End If
                                        Console.WriteLine("            SR : " + showName)

                                        If doEDITS Then
showEditThreatAgain3:
                                            Console.WriteLine("Keep this security requirement on this component? (y/n/desc)")
                                            Dim keepThreat As Boolean = True
                                            Dim result = Console.ReadKey()
                                            Console.SetCursorPosition(0, Console.CursorTop - 1)
                                            Console.WriteLine(spaces(80))
                                            Console.SetCursorPosition(0, Console.CursorTop - 1)

                                            If LCase(result.KeyChar.ToString) = "d" Then
                                                Console.WriteLine(T.lib_SR(sNdx).Description)
                                                GoTo showEditThreatAgain3
                                            End If

                                            If LCase(result.KeyChar.ToString) = "n" Then

                                                If isDUP = True Then
                                                    Console.WriteLine("** This SR is linked to multiple threats of this Component **")
                                                    Console.WriteLine("Press 'O' to OVERRIDE & remove this SR from all of this component's threats..")
                                                    result = Console.ReadKey()
                                                    Console.SetCursorPosition(0, Console.CursorTop - 2)
                                                    Console.WriteLine(spaces(120))
                                                    Console.WriteLine(spaces(120))
                                                    Console.SetCursorPosition(0, Console.CursorTop - 2)
                                                    If result.KeyChar.ToString <> "O" Then
                                                        GoTo escapeToHere
                                                    End If
                                                End If

                                                'Console.WriteLine(vbCrLf + "Removing SR from component")
                                                If T.removeLabelFromSR(T.lib_SR(sNdx), COMP.Name) Then
                                                    '
                                                    Call T.addEditSR(T.lib_SR(sNdx))
                                                    changeS.Add("Removed SR from Threat " + thR.Name + " [" + thR.Id.ToString + "] - '" + COMP.Name + "' label removed from SR: " + T.lib_SR(sNdx).Name + " [" + T.lib_SR(sNdx).Id.ToString + "]")

                                                Else
                                                    Console.WriteLine("Could not find label '" + COMP.Name + "' on SR")
                                                End If
                                            Else
                                                'Console.WriteLine(vbCrLf + "Keeping SR on component")
                                            End If
escapeToHere:
                                        End If

                                    End If
                                Next

                            Next
                        End If

                    Next
skipSRsFromThreatATTR:

                Next
nope:

            End If
        End With
        Console.WriteLine("==============================")

        If doEDITS = True And changeS.Count > 0 Then
            Console.WriteLine("Changes made: " + changeS.Count.ToString + " total" + vbCrLf)
            For Each cc In changeS
                Console.WriteLine(cc)
            Next
        End If

    End Sub



    Private Function loginItem(ByRef L As String, ByVal itemNum As Integer) As String
        loginItem = ""

        Dim lDetails() As String = Split(L, "|")
        If UBound(lDetails) < 2 Then Exit Function

        If itemNum = 0 Then
            Return lDetails(0)
        Else
            Dim S3 As New Simple3Des("7w6e87twryut24876wuyeg")
            loginItem = S3.Decode(lDetails(itemNum))
        End If

        GC.Collect()
    End Function

    Private Sub cloneComponent(C As tmComponent, T2 As TM_Client)
        ' if DEEP is true, all items of components are created inside user's default library whether they exist or not (as defined by source instance)
        ' if DEEP is false, attempt to match to GUIDs, if GUID unavailable attempt to match to TEXT. if neither is available, either report (build=false) or build
        ' buildNecessary = true/ will build all necessary objects to complete component design
        ' Threats
        ' SRs of Threats (incl label matching)
        ' DirectSRs (NOT DONE YET)
        ' Attributes
        ' Threats of Attributes
        ' SRs of Threats of Attributes

        Dim destC As tmComponent

        destC = T2.bestMatch(C)      '( T2.guidCOMP(C.Guid.ToString)

        Dim targetID As Integer = 0
        targetID = destC.Id

        If destC.Id = 0 Then
            Console.WriteLine("Cannot find existing Component")
        End If

        ' Look up library name by text at Target.. set library ID of target
        Dim libLoop$ = ""
        libLoop = T.librarieS(T.ndxLib(C.LibraryId)).Name

        Dim T2libID As Integer = 0
        T2libID = T2.ndxLibByName(libLoop, T2.librarieS)

        Dim targetLIBid As Integer = 10 'default to CORP

        If T2libID <> -1 Then targetLIBid = T2.librarieS(T2libID).Id
        ' target library will be specified, though will be corp until API allows library specification

        targetLIBid = 10

        Dim ndxCompType As Integer = 0
        Dim targetCompTypeId As Integer = 0
        Dim cTypeS$ = ""

        cTypeS = T.componentTypes(T.ndxCompType(C.ComponentTypeId)).Name
        targetCompTypeId = T2.ndxCompTypeByName(cTypeS)

        If targetCompTypeId = -1 Then
            targetCompTypeId = T2.componentTypes(T2.ndxCompTypeByName("Component")).Id
            Console.WriteLine("Changing from CompType '" + cTypeS + "' to 'Component'")
            cTypeS = "Component"
        Else
            targetCompTypeId = T2.componentTypes(T2.ndxCompTypeByName(cTypeS)).Id
        End If

        If targetID <> 0 Then
            ' we need to add a suffix to the name and/or hide the original
            Call removeallSRfromcomp(destC, T2)

            Console.WriteLine("Hiding original component at " + T2.tmFQDN + "- " + T2.hideItem(destC).ToString)
            C.IsHidden = False
            C.Name = Replace(C.Name, "_TM", "")
        End If

        Console.WriteLine(C.CompName + " [" + C.CompID.ToString + "] - ADDING: " + T2.addEditCOMP(C,, targetLIBid, targetCompTypeId, cTypeS).ToString)
        Call loadNTY(T2, "Components")

            destC = T2.bestMatch(C)


haveDestC:

        Call T.buildCompObj(C)

        For Each tH In C.listThreats
            Dim destTH As tmProjThreat

            Call writeObj(tH, T)
            destTH = T2.bestMatch(tH, False) 'T2.guidTHREAT(tH.Guid.ToString)

            If destTH.Id = 0 Then
                Console.WriteLine("ADDING TO TF  : " + destTH.Name + " [" + destTH.Id.ToString + "] - " + T2.addTH(destTH).ToString)
                Call loadNTY(T2, "Threats")
                destTH = T2.bestMatch(destTH, False)
            End If
            If destTH.Id <> 0 Then
                Call T2.addThreatToComponent(destC, destTH.Id)
                Console.WriteLine("ADDING TO COMP: i1 ID " + tH.Id.ToString + "   i2 ID " + destTH.Id.ToString)
            End If

            For Each S In tH.listLinkedSRs
                Dim sourceS As tmProjSecReq
                Dim ndxS As Integer = T.ndxSRlib(S)
                sourceS = T.lib_SR(ndxS)

                Dim destS As tmProjSecReq
                Call writeObj(sourceS, T) '  < why doesnt this work
                destS = T2.bestMatch(sourceS, False)

                If destS.Id = 0 Then
                    Console.WriteLine(">>ADDING TO TF    : " + sourceS.Name + " [" + sourceS.Id.ToString + "] - " + T2.addSR(sourceS).ToString)
                    Call loadNTY(T2, "SecurityRequirements")
                    destS = T2.bestMatch(sourceS, False)
                End If

                If destS.Id <> 0 Then
                    Console.WriteLine(">>ADDING TO THREAT: i1 ID " + tH.Id.ToString + "   i2 ID " + destS.Id.ToString)
                    Call T2.addSRtoThreat(destTH, destS.Id)
                    If T2.matchLabelsOnSR(destS, C.Labels) Then
                        Call T.addEditSR(destS)
                    End If
                End If
            Next
skipSRs:
            Console.WriteLine("-----------------------------")
        Next

        ' now the attributes
        ' make sure ATTR is viable
        For Each A In C.listAttr
            Dim destA As tmAttribute
            destA = T2.bestMatch(A, False)
            If destA.Id = 0 Then
                Dim isViable As Boolean = True
                If A.Options.Count <> 2 Then
                    isViable = False
                End If
                If A.Options(0).Name <> "Yes" Then
                    isViable = False
                End If


                If isViable Then
                    Console.WriteLine("Adding ATTR [" + A.Id.ToString + "] " + A.Name)
                    T2.addEditATTR(A)
                    loadNTY(T2, "Attributes")
                Else
                    Console.WriteLine("ERROR: This ATTR cannot be created via API currently")
                    GoTo nextATTR
                End If
            Else
                Console.WriteLine("Adding to Component " + C.Name + ": ATTR [" + A.Id.ToString + "] " + A.Name)
                T2.addAttributeToComponent(destC, destA.Id)
            End If

            For Each O In A.Options
                For Each oTh In O.Threats
                    Dim destTH As tmProjThreat

                    Dim Th As New tmProjThreat
                    Dim ndxT As Integer = T.ndxTHlib(oTh.Id)
                    Th = T.lib_TH(ndxT)
                    'Call writeObj(Th, T)
                    ' look up threats by lib_th

                    destTH = T2.bestMatch(Th, True) 'T2.guidTHREAT(tH.Guid.ToString)

                    If destTH.Id = 0 Then
                        Console.WriteLine("ADDING TO TF  : " + Th.Name + " [" + Th.Id.ToString + "] - " + T2.addTH(Th).ToString)
                        Call loadNTY(T2, "Threats")
                        destTH = T2.bestMatch(destTH, False)
                    Else
                        Console.WriteLine("THREAT EXISTS.. [" + destTH.Id.ToString + "/" + destTH.Guid.ToString + "] " + destTH.Name)
                    End If
                    If destTH.Id <> 0 Then

                        Call T2.addThreatToAttribute(destA, destTH.Id)
                        Console.WriteLine("ADDING TO ATTR: i1 ID " + Th.Id.ToString + "   i2 ID " + destTH.Id.ToString)
                    End If

                    Dim atSRs As New Collection
                    Dim ndxS As Integer = 0
                    atSRs = T.returnSRsWithLabelMatch(Th, C)
                    For Each S In atSRs
                        ndxS = T.ndxSRlib(S)
                        Dim destS As tmProjSecReq
                        destS = T2.bestMatch(T.lib_SR(ndxS))

                        If destS.Id = 0 Then
                            Console.WriteLine(">>ADDING TO TF    : " + T.lib_SR(ndxS).Name + " [" + T.lib_SR(ndxS).Id.ToString + "] - " + T2.addSR(T.lib_SR(ndxS)).ToString)
                            Call loadNTY(T2, "SecurityRequirements")
                            destS = T2.bestMatch(T.lib_SR(ndxS), False)
                        End If

                        If destS.Id <> 0 Then
                            Console.WriteLine(">>ADDING TO THREAT: i1 ID " + Th.Id.ToString + "   i2 ID " + destS.Id.ToString)
                            Call T2.addSRtoThreat(destTH, destS.Id)
                            If T2.matchLabelsOnSR(destS, C.Labels) Then
                                Call T.addEditSR(destS)
                            End If
                        End If

                    Next

                Next
            Next

nextATTR:
        Next





    End Sub



    Private Sub cloneComponent2(C As tmComponent, T2 As TM_Client, Optional ByVal deepClone As Boolean = False, Optional ByVal buildNecessaryObjects As Boolean = True, Optional ByVal reportOnly As Boolean = True)
        'Per Nik 12/6 hide strategy to coponent only/ map bestmatch and/or create for others

        ' if DEEP is true, all items of components are created inside user's default library whether they exist or not (as defined by source instance)
        ' if DEEP is false, attempt to match to GUIDs, if GUID unavailable attempt to match to TEXT. if neither is available, either report (build=false) or build
        ' buildNecessary = true/ will build all necessary objects to complete component design
        ' Threats
        ' SRs of Threats (incl label matching)
        ' DirectSRs (NOT DONE YET)
        ' Attributes
        ' Threats of Attributes
        ' SRs of Threats of Attributes

        Console.WriteLine("DEEP=" + deepClone.ToString + ": When true, build a copy of component with all associated objects - otherwise, use best match whenever possible")
        Console.WriteLine("BUILD=" + buildNecessaryObjects.ToString + ": When DEEPCLONE is False, setting BUILD=TRUE will build a copy of every object not found via GUID or NAME" + vbCrLf)
        Dim destC As tmComponent

        destC = T2.bestMatch(C)      '( T2.guidCOMP(C.Guid.ToString)

        Dim targetID As Integer = 0
        targetID = destC.Id

        If destC.Id = 0 Then
            Console.WriteLine("Cannot find existing Component")
        End If


        If deepClone = True Then
            ' here build the component and then pull new T2.lib_comp to set DESTC
            ' should pull componentID from add routine

            If targetID <> 0 Then
                ' we need to add a suffix to the name and/or hide the original
                Console.WriteLine("Hiding original component at " + T2.tmFQDN + "- " + T2.hideItem(destC).ToString)
                C.IsHidden = False
                C.Name = Replace(C.Name, "_hidden", "")
            End If

            Console.WriteLine(C.CompName + " [" + C.CompID.ToString + "] - ADDING: " + T2.addEditCOMP(C).ToString)
            Call loadNTY(T2, "Components")

            destC = T2.bestMatch(C)

        Else
            If targetID = 0 Then
                ' if here, unable to find by name or GUID
                If buildNecessaryObjects = False Then
                    Console.WriteLine("Exiting routine - need to set BUILD=TRUE to add Component to " + T2.tmFQDN)
                    Exit Sub
                End If
            End If
        End If


        If destC.Id = 0 Then
            Console.WriteLine("Cannot find Component - unable to add") ' via NAME - must exit here")
            Exit Sub
        Else
            targetID = destC.Id
        End If

haveDestC:

        Call T.buildCompObj(C)

        For Each tH In C.listThreats
            Dim destTH As tmProjThreat

            Call writeObj(tH, T)
            destTH = T2.bestMatch(tH, False) 'T2.guidTHREAT(tH.Guid.ToString)

            If deepClone = True Then
                'Console.WriteLine("Here will HIDE and ADD/ DEEP is TRUE")
                If destTH.Id <> 0 Then
                    Console.WriteLine("HIDING/RENAMING original threat at " + T2.tmFQDN + "- " + T2.hideItem(destTH).ToString)
                    destTH.IsHidden = False
                    destTH.Name = Replace(destTH.Name, "_hidden", "")
                End If
                Console.WriteLine("ADDING TO TF  : " + destTH.Name + " [" + destTH.Id.ToString + "] - " + T2.addTH(destTH).ToString)
                Call loadNTY(T2, "Threats")
                destTH = T2.bestMatch(destTH, False)
            Else
                If destTH.Id = 0 And buildNecessaryObjects = True Then
                    Console.WriteLine("ADDING TO TF  : " + destTH.Name + " [" + destTH.Id.ToString + "] - " + T2.addTH(destTH).ToString)
                    Call loadNTY(T2, "Threats")

                    destTH = T2.bestMatch(destTH, False)
                End If
            End If

            If destTH.Id <> 0 Then
                Call T2.addThreatToComponent(destC, destTH.Id)
                Console.WriteLine("ADDING TO COMP: i1 ID " + tH.Id.ToString + "   i2 ID " + destTH.Id.ToString)
            End If

            For Each S In tH.listLinkedSRs
                Dim sourceS As tmProjSecReq
                Dim ndxS As Integer = T.ndxSRlib(S)
                sourceS = T.lib_SR(ndxS)

                Dim destS As tmProjSecReq
                Call writeObj(sourceS, T) '  < why doesnt this work
                'Console.WriteLine(">>SR:" + sourceS.Name)
                destS = T2.bestMatch(sourceS, True)

                If deepClone = True Then
                    'Console.WriteLine("Here will HIDE and ADD/ DEEP is TRUE")
                    If destS.Id <> 0 Then
                        Console.WriteLine(">>HIDING/RENAMING original SR at " + T2.tmFQDN + "- " + T2.hideItem(destS).ToString)
                        destS.IsHidden = False
                        destS.Name = Replace(destS.Name, "_hidden", "")
                    End If
                    Console.WriteLine(">>ADDING TO TF    : " + sourceS.Name + " [" + sourceS.Id.ToString + "] - " + T2.addSR(sourceS).ToString)
                    Call loadNTY(T2, "SecurityRequirements")
                    destS = T2.bestMatch(sourceS, False)
                Else
                    If destS.Id = 0 And buildNecessaryObjects = True Then
                        Console.WriteLine(">>ADDING TO TF    : " + sourceS.Name + " [" + sourceS.Id.ToString + "] - " + T2.addSR(sourceS).ToString)
                        Call loadNTY(T2, "SecurityRequirements")

                        destS = T2.bestMatch(sourceS, False)
                    End If
                End If

                If destS.Id <> 0 Then
                    Call T2.addSRtoThreat(destTH, destS.Id)
                    Console.WriteLine(">>ADDING TO THREAT: i1 ID " + tH.Id.ToString + "   i2 ID " + destS.Id.ToString)
                End If
            Next
skipSRs:
            Console.WriteLine("-----------------------------")
        Next




    End Sub

    Private Sub writeObjComp(sourceC As Object, destC As Object, T As TM_Client, Optional ByVal shorTdesc As Boolean = True)
        Dim tStr$ = T.entityType(sourceC)

        Console.WriteLine(sourceC.Name + " [" + sourceC.Id.ToString + "/" + sourceC.Guid.ToString + "] in " + tStr + " [" + destC.Id.ToString + "/" + destC.Guid.ToString + "] of " + T.tmFQDN) ' - adding " + C.listThreats.Count.ToString + " Threats")

    End Sub
    Private Sub writeObj(C As Object, T As TM_Client, Optional ByVal shorTdesc As Boolean = True)
        Dim tStr$ = T.entityType(C)

        Console.WriteLine(tStr + ": [" + C.Id.ToString + "/" + C.Guid.ToString + "] " + C.name)

    End Sub

    Private Sub allCompMappings()
        Dim R As tfRequest
        R = New tfRequest
        With R
            .EntityType = "Threats"
            .LibraryId = 0
            .ShowHidden = False
        End With
        T.lib_TH = T.getTFThreats(R)
        '        T2.lib_TH = T2.getTFThreats(R)

        R.EntityType = "Components"
        T.lib_Comps = T.getTFComponents(R)
        'T2.lib_Comps = T2.getTFComponents(R)

        R.EntityType = "SecurityRequirements"
        T.lib_SR = T.getTFSecReqs(R)
        ' T2.lib_Comps = T2.getTFComponents(R)

        R.EntityType = "Attributes"
        T.lib_AT = T.getTFAttr(R)
        'T2.lib_AT = T2.getTFAttr(R)

        Console.WriteLine("Loaded everything from " + T.tmFQDN)

        Dim numCompsWithAT As Integer = 0
        Dim firstAT As Boolean = True
        Dim compsWith As New Collection

        For Each C In T.lib_Comps
            Console.WriteLine("Building " + C.Name + " [" + C.Id.ToString + "/" + C.Guid.ToString + "]") '] is Comp ID " + destC.Id.ToString + " on i2") ' - # ATTR: " + C.listAttr.Count.ToString) ' - adding " + C.listThreats.Count.ToString + " Threats")
            Call T.buildCompObj(C,,, True)
            If C.listAttr.Count = 0 Then GoTo skipComp2

            Dim atStr$ = ""
            For Each AT In C.listAttr
                atStr += AT.Id.ToString + ","
            Next
            atStr = Mid(atStr, 1, Len(atStr) - 1)

            compsWith.Add("COMP:" + C.Id.ToString + "/" + C.Name + " ATTR:" + atStr)
            numCompsWithAT += 1

skipComp2:

        Next

        Console.WriteLine("# of Components with Attributes: " + numCompsWithAT.ToString)
        For Each cc In compsWith
            Console.WriteLine(cc)
        Next
        End


    End Sub







    ' ----- MODELS --- Show models & # appearances - put into tmComponent 'modelsPresent' and 'numAppearances' properties


    Public Sub loadComponentUsage(TM As TM_Client, AllProj As List(Of tmProjInfo))
        ' assumes components are loaded into TM
        Dim numProj As Integer = 0

        For Each PP In AllProj
            numProj += 1
            Console.WriteLine("Checking Model " + numProj.ToString + ": " + PP.Name)
            Dim P As tmModel = New tmModel
            P = TM.getProject(PP.Id)

            Console.SetCursorPosition(0, Console.CursorTop - 1)
            Console.WriteLine(spaces(80))
            Console.SetCursorPosition(0, Console.CursorTop - 1)

            For Each N In P.Nodes
                For Each C In TM.lib_Comps
                    If C.Id = N.ComponentId Then
                        Dim currModelID As Integer = 0 'if 0, does not exist

                        currModelID = C.getModelNDX(PP.Id)
                        If currModelID = -1 Then
                            C.modelsPresent.Add(PP.Id)
                            C.numInstancesPerModel.Add(1)
                        Else
                            C.numInstancesPerModel(currModelID) += 1
                        End If
                    End If
                Next
            Next
            Console.WriteLine(spaces(80))
            Console.SetCursorPosition(0, Console.CursorTop - 1)

        Next

    End Sub




    Private Sub loadNTY(clienT As TM_Client, ByVal ntyType$)
        Dim R As New tfRequest

        With R
            .LibraryId = 0
            .ShowHidden = False
        End With

        Select Case LCase(ntyType)
            Case "components"
                R.EntityType = "Components"
                clienT.lib_Comps = clienT.getTFComponents(R)
            Case "threats"
                R.EntityType = "Threats"
                clienT.lib_TH = clienT.getTFThreats(R)
            Case "securityrequirements"
                R.EntityType = "SecurityRequirements"
                clienT.lib_SR = clienT.getTFSecReqs(R)
            Case "attributes"
                R.EntityType = "Attributes"
                clienT.lib_AT = clienT.getTFAttr(R)
            Case "componenttypes"
                R.EntityType = "ComponentTypes"
                clienT.componentTypes = clienT.getEntityMisc(R)
            Case "roles"
                R.EntityType = "Roles"
                clienT.roleS = clienT.getEntityMisc(R)
            Case "dataelements"
                R.EntityType = "DataElements"
                clienT.dataElements = clienT.getEntityMisc(R)

        End Select
        'Console.WriteLine("Loaded " + R.EntityType + " from " + clienT.tmFQDN)
    End Sub
    Private Function addTF_Attr(sourceT As TM_Client, destT As TM_Client, AT As tmAttribute, Optional ByVal deeP As Boolean = False) As Boolean
        addTF_Attr = False
        If AT.Options.Count <> 2 Then
            Console.WriteLine("This attribute not compatible with API")
            Exit Function
        End If

        If AT.Options(0).Name <> "Yes" Then
            Console.WriteLine("This attribute not compatible with API/ option 1 not YES")
            Exit Function
        End If

        Call destT.addEditATTR(AT)
        Console.WriteLine("ATTR added to " + destT.tmFQDN + ": " + AT.Name)

        'here may need to change/hide original

        Dim destA As tmAttribute
        Call loadNTY(destT, "Attributes")

        Dim ndxA As Integer = destT.ndxATTRbyName(AT.Name)
        destA = destT.lib_AT(ndxA)

        With AT.Options(0)
            For Each tH In .Threats
                Dim destTH As tmProjThreat
                Dim ndxT As Integer = sourceT.ndxTHlib(tH.Id)
                ' have to do this because attributes dont pass the GUID of the Threat
                Dim libT As tmProjThreat = sourceT.lib_TH(ndxT)


                Console.WriteLine("     ADD TH: i1:" + tH.Id.ToString + "   i2:" + destTH.Id.ToString)
                destT.addThreatToAttribute(destA, destTH.Id)
            Next
        End With

        addTF_Attr = True
    End Function

    Private Function findLocalMatch(cID As Integer, cNAME$) As tmComponent
        Dim ndxC As Integer = 0
        findLocalMatch = New tmComponent

        Dim C As tmComponent
        If cID Then
            ndxC = T.ndxComp(cID)
            If ndxC = -1 Then
                Console.WriteLine("Component does not exist")
                End
            End If
            Console.WriteLine("Found Component " + cID.ToString + ": " + T.lib_Comps(ndxC).Name + " [" + T.lib_Comps(ndxC).CompID.ToString + "] at " + T.tmFQDN)
            C = T.lib_Comps(ndxC)
        Else
            ndxC = T.ndxCompbyName(cNAME)
            If ndxC = -1 Then
                Console.WriteLine("Component does not exist")
                End
            End If
            Console.WriteLine("Found Component: " + T.lib_Comps(ndxC).Name + " [" + T.lib_Comps(ndxC).CompID.ToString + "] at " + T.tmFQDN)
            C = T.lib_Comps(ndxC)
        End If

        Return C
    End Function
    Private Sub giveHelp()
        Console.WriteLine("USAGE: TMCLI action --param1 param1_value --param2 param2_value" + vbCrLf)
        Console.WriteLine("ACTIONS:")
        Console.WriteLine("--------")
        Console.WriteLine(fLine("help", "Produces this list of actions and parameters"))

        Console.WriteLine(fLine("makelogin", "Create Login File, args: --FQDN (demo2.thr...), --UN, --PW"))

        Console.WriteLine(vbCrLf + "Accelerator APIs" + vbCrLf + "==============================")
        Console.WriteLine(fLine("show_aws_iam", "Show all available AWS IAM accounts"))
        Console.WriteLine(fLine("show_vpc", "Show VPCs of an account, arg: --AWSID (Id)"))
        Console.WriteLine(fLine("create_vpc_model", "Create a model from a VPC, arg: --VPCID (Id)"))

        Console.WriteLine(vbCrLf + "Instance Info" + vbCrLf + "==============================")
        'Console.WriteLine(fLine("get_notes", "Returns notes associated with all components"))
        Console.WriteLine(fLine("get_groups", "Returns Groups"))
        Console.WriteLine(fLine("compliance_csv", "Returns Compliance Framework/SR mapping, arg: --FILE, OPT: --SUMMARY true"))
        Console.WriteLine(fLine("get_projects", "Returns Projects, OPTIONAL --FILE (csv filename)"))
        Console.WriteLine(fLine("get_users", "Returns Users, OPTIONAL --FILE (csv filename), --DEPT (id), --LASTLOGIN (days since last login)"))

        Console.WriteLine(fLine("get_libs", "Returns Libraries"))
        Console.WriteLine(fLine("get_labels", "Returns Labels, arg: --ISSYSTEM (True/False)"))
        Console.WriteLine(fLine("summary", "Returns a summary of all Threat Models"))
        Console.WriteLine(fLine("template_convert", "text"))

        Console.WriteLine(vbCrLf + "Local TF Info" + vbCrLf + "==============================")

        Console.WriteLine(fLine("show_comp", "Returns list of Components, optional args: --LIB (library name),--FILE (csv filename)"))
        Console.WriteLine(fLine("get_comp", "Returns details of Component, use arg: --ID (component ID) or --NAME (component name)) OPT: --EDIT true"))
        Console.WriteLine(fLine("get_threats", "Returns threat list or single threat, OPT arg: --ID (component ID), OPT --SEARCH text,--FILE (csv filename)"))
        Console.WriteLine(fLine("get_attr", "Returns list of attributes or single attribute, OPT --ID Id OPT arg: --SEARCH text"))
        Console.WriteLine(fLine("get_sr", "Returns list of SRs or single Security Requirement, OPT --ID Id OPT arg: --SEARCH text,--FILE (csv filename)"))
        Console.WriteLine(fLine("attr_report", "Shows all entity types and number of threats - dumps to a file --ID (filename),--FILE (csv filename)"))
        Console.WriteLine(fLine("compattr_mapping", "Shows results of comp_attr_mapping file (set by default) used in other cmds, like find_dups (attr)"))
        Console.WriteLine(fLine("comp_compare", "text"))
        Console.WriteLine(fLine("find_dups", "Shows duplicates of entity object - with ATTR gives SQL for deletion, require --ENTITY (threats/securityreq..etc)"))


        Console.WriteLine(vbCrLf + "Local TF Editing" + vbCrLf + "==============================")
        Console.WriteLine(fLine("addcomp_threat", "Add a Threat and choose SRs for a Component, use arg: --ID (component ID) or --NAME (component name)) --THREATID (id)"))
        Console.WriteLine(fLine("addcomp_attr", "Add an Attribute, its Threats and choose SRs for a Component, use arg: --ID (component ID) or --NAME (component name)) --THREATID (id)"))
        Console.WriteLine(fLine("addattr_threat", "Add a Threat and choose SRs for an Attribute, use arg: --ID (attribute ID) --THREATID (id)"))
        Console.WriteLine(fLine("addthreat_sr", "Add a Security Requirement to a Threat, use arg: --ID (threat ID) --SRID (SR ID)"))
        Console.WriteLine(fLine("subthreat_sr", "Remove a Security Requirement from a Threat, use arg: --ID (threat ID) --SRID (SR ID)"))
        Console.WriteLine(fLine("compattr_mappings", "Use to create file comp_template_mappings file of attribute mapping"))
        Console.WriteLine(fLine("addcomp_comp", "text"))
        Console.WriteLine(fLine("sql_clean_roles_elements_widgets", "Produces SQL to clean out Entities & Labels"))

        Console.WriteLine(vbCrLf + "Instance-to-Instance (use param --I2 (fqdn) for all calls)" + vbCrLf + "=================================================")
        Console.WriteLine(fLine("i2i_threatloop_addsr", "text"))
        Console.WriteLine(fLine("i2i_attrloop_addattr", "text"))
        Console.WriteLine(fLine("i2i_attrloop_addth", "text"))
        Console.WriteLine(fLine("i2i_clonecomp", "Deep clone - component and all associated Threats/SRs added as necessary, use either --LIBRARY (lib name), or --ID, --NAME (component)"))
        Console.WriteLine(fLine("i2i_comploop_addth", "text"))
        Console.WriteLine(fLine("i2i_comploop_addattr", "text"))
        Console.WriteLine(fLine("i2i_lib_compare", "text"))
        Console.WriteLine(fLine("i2i_sql_libs", "text"))
        Console.WriteLine(fLine("template_convert", "text"))
        Console.WriteLine(fLine("i2i_bestmatch", "text"))
        Console.WriteLine(fLine("i2i_allcomp_compare", "Deep discovery of differences between instances"))
        Console.WriteLine(fLine("i2i_allsr_compare", "Deep discovery of differences between instances"))
        Console.WriteLine(fLine("i2i_add_comps", "Adds every Component from default instance to i2, use arg: --I2 (fqdn_of_target)"))
        Console.WriteLine(fLine("i2i_add_srs", "Adds every SR from default instance to i2, use arg: --I2 (fqdn_of_target)"))
        Console.WriteLine(fLine("i2i_add_threats", "Adds every Threat from default instance to i2, use arg: --I2 (fqdn_of_target), opt: --id (copy_only_ID)"))
        Console.WriteLine(fLine("i2i_allcomp_loop", "Creates SQL dump to match GUID according to Name"))
        Console.WriteLine(fLine("i2i_entitylib_sql", "Creates SQL for matching Library ID to GUID (make sure GUIDs are correct first) for all entities (only MH to use)"))
        Console.WriteLine(fLine("i2i_allsr_loop", "Creates SQL dump to match GUID according to Name"))
        Console.WriteLine(fLine("i2i_allthreat_loop", "Creates SQL dump to match GUID according to Name"))
        Console.WriteLine(fLine("i2i_allcomp_update", "Shows differences in components between 2 instances (update testing)"))
        Console.WriteLine(fLine("i2i_comp_compare", "Shows difference in a single component across instances, use --ID or --NAME (component)"))


    End Sub

End Module
