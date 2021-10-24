﻿Imports RestSharp
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Web.HttpUtility
Public Class tmWrapper

End Class

Public Class genJSONobj
    Public isSuccess$
    Public Message$
    Public Data$
End Class
Public Class TM_Client
    Public lastError$ = ""
    Public tmFQDN$ 'ie http://localhost https://myserver.myzone.com
    Public isConnected As Boolean
    Private slToken$

    Public lib_Comps As List(Of tmComponent)
    Public lib_TH As List(Of tmProjThreat)
    Public lib_SR As List(Of tmProjSecReq)
    Public lib_AT As List(Of tmAttribute)

    Public sysLabels As List(Of tmLabels)
    Public labelS As List(Of tmLabels)
    Public groupS As List(Of tmGroups)
    Public librarieS As List(Of tmLibrary)


    Public Sub New(fqdN$, uN$, pw$)

        tmFQDN = fqdN

        Me.isConnected = False

        'Console.WriteLine("New TM_Client activated: " + Replace(tmFQDN, "https://", ""))

        Console.WriteLine("Connecting to " + fqdN)
        Dim client = New RestClient(fqdN + "/token")
        Dim request = New RestRequest(Method.POST)
        Dim response As IRestResponse

        client.Timeout = -1

        request.RequestFormat = DataFormat.None

        request.AddHeader("Content-Type", "application/x-www-form-urlencoded")
        request.AddParameter("username", uN)
        request.AddParameter("password", pw)
        request.AddParameter("grant_type", "password")

        response = client.Execute(request)
        Console.WriteLine("Login non-SSO method (demo2): " + response.ResponseStatus.ToString + "/ " + response.StatusCode.ToString + "/ Success: " + response.IsSuccessful.ToString)

        If response.IsSuccessful = False Then
            client = New RestClient(fqdN + "/idsvr/connect/token")
            request = New RestRequest(Method.POST)
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded")
            request.AddHeader("Accept", "application/json")

            request.AddParameter("username", uN)
            request.AddParameter("password", pw)
            request.AddParameter("grant_type", "password")
            request.AddParameter("client_id", "demo-resource-owner")
            request.AddParameter("client_secret", "geheim")
            request.AddParameter("scope", "openid profile email api")

            response = client.Execute(request)
            Console.WriteLine("Login SSO method (/idsvr/connect): " + response.ResponseStatus.ToString + "/ " + response.StatusCode.ToString + "/ Success: " + response.IsSuccessful.ToString)

        End If

        If response.IsSuccessful = True Then
            Me.isConnected = True
            Dim O As JObject = JObject.Parse(response.Content)
            slToken = O.SelectToken("access_token")
            Exit Sub
        End If

        If IsNothing(response.ErrorMessage) = False Then
            lastError = response.ErrorMessage.ToString
        Else
            lastError = response.Content
        End If

        Console.WriteLine(lastError)
        'Console.WriteLine("Retrieved access token")
    End Sub

    Private Function getAPIData(ByVal urI$, Optional ByVal usePOST As Boolean = False, Optional ByVal addJSONbody$ = "") As String
        getAPIData = ""
        If isConnected = False Then Exit Function

        Dim client = New RestClient(tmFQDN + urI)
        Dim request As RestRequest
        If usePOST = False Then request = New RestRequest(Method.GET) Else request = New RestRequest(Method.POST)

        Dim response As IRestResponse

        request.AddHeader("Authorization", "Bearer " + slToken)
        request.AddHeader("Accept", "application/json")

        If Len(addJSONbody) Then
            request.AddHeader("Content-Type", "application/json")
            request.AddHeader("Accept-Encoding", "gzip, deflate, br")

            request.AddHeader("Accept-Language", "zh")

            request.AddHeader("Connection", "keep-alive")
            request.AddParameter("application/json", addJSONbody, ParameterType.RequestBody)
        End If

        response = client.Execute(request)

        Dim O As JObject = JObject.Parse(response.Content)

        If IsNothing(O.SelectToken("IsSuccess")) = True Then
            Console.WriteLine("API Request Rejected: " + urI)
            getAPIData = ""
            Exit Function
        End If

        If CBool(O.SelectToken("IsSuccess")) = False Then
            getAPIData = "ERROR:Could not retrieve " + urI
            Exit Function
        End If

        Return O.SelectToken("Data").ToString
    End Function

    Public Function getLibraries() As List(Of tmLibrary)
        getLibraries = New List(Of tmLibrary)
        Dim jsoN$ = getAPIData("/api/libraries")

        If jsoN = "" Then
            Return getLibraries 'returning an empty object
            Exit Function
        End If

        getLibraries = JsonConvert.DeserializeObject(Of List(Of tmLibrary))(jsoN)

    End Function


    Public Function getGroups() As List(Of tmGroups)
        getGroups = New List(Of tmGroups)
        Dim jsoN$ = getAPIData("/api/groups")

        getGroups = JsonConvert.DeserializeObject(Of List(Of tmGroups))(jsoN)

    End Function
    Public Function getAWSaccounts() As List(Of tmAWSacc)
        getAWSaccounts = New List(Of tmAWSacc)
        Dim jsoN$ = getAPIData("/api/thirdparty/list/AWS")
        getAWSaccounts = JsonConvert.DeserializeObject(Of List(Of tmAWSacc))(jsoN)

    End Function
    Public Function getVPCs(awsID As Long) As List(Of tmVPC)
        On Error GoTo errorcatch

        getVPCs = New List(Of tmVPC)
        Dim jsoN$ = getAPIData("/api/thirdparty/account/" + awsID.ToString + "/AWS")
        Dim O As JObject = JObject.Parse(jsoN)
        jsoN = O.SelectToken("VPCs").ToString
        getVPCs = JsonConvert.DeserializeObject(Of List(Of tmVPC))(jsoN)

errorcatch:
        'empty JSON returns empty list
    End Function


    Public Function getAllProjects() As List(Of tmProjInfo)
        getAllProjects = New List(Of tmProjInfo)
        '        Dim jsoN$ = getAPIData("/api/projects/GetAllProjects")
        Dim c$ = Chr(34)
        Dim bodY$ = "{" + c + "virtualGridStateRequestModel" + c + ":{" + c + "state" + c + ":{" + c + "skip" + c + ":0," + c + "take" + c + ":100}}," + c + "projectFilterModel" + c + ":{" + c + "ActiveProjects" + c + ":true}}"

        Dim jsoN$ = getAPIData("/api/projects/smartfilter", True, bodY)

        Dim O As JObject = JObject.Parse(jsoN)
        jsoN = O.SelectToken("Data").ToString


        getAllProjects = JsonConvert.DeserializeObject(Of List(Of tmProjInfo))(jsoN)

    End Function


    Public Function getLabels(Optional ByVal isSystem As Boolean = False) As List(Of tmLabels)
        getLabels = New List(Of tmLabels)
        Dim jsoN$ = getAPIData("/api/labels?isSystem=" + LCase(CStr(isSystem))) 'true") '?isSystem=false")

        getLabels = JsonConvert.DeserializeObject(Of List(Of tmLabels))(jsoN)
    End Function

    Public Function getProjectsOfGroup(G As tmGroups) As List(Of tmProjInfo)
        getProjectsOfGroup = New List(Of tmProjInfo)

        Dim jBody$ = ""
        jBody = JsonConvert.SerializeObject(G)
        Dim jsoN$ = getAPIData("/api/group/projects", True, jBody$)

        getProjectsOfGroup = JsonConvert.DeserializeObject(Of List(Of tmProjInfo))(jsoN)
    End Function

    Public Function prepVPCmodel(G As tmVPC) As String
        prepVPCmodel = ""

        Dim jBody$ = ""
        jBody = JsonConvert.SerializeObject(G)
        Return "[" + jBody + "]"
    End Function

    Public Sub createVPCmodel(G As tmVPC)
        On Error Resume Next
        'this command times out - should be async

        Call getAPIData("/api/thirdparty/createthreatmodelfromvpc", True, prepVPCmodel(G))
    End Sub


    Public Function getTFComponents(T As tfRequest) As List(Of tmComponent)
        getTFComponents = New List(Of tmComponent)

        Dim jBody$ = ""
        jBody = JsonConvert.SerializeObject(T)

        Dim jsoN$ = getAPIData("/api/threatframework", True, jBody$)

        getTFComponents = JsonConvert.DeserializeObject(Of List(Of tmComponent))(jsoN)
    End Function


    Public Function getTFSecReqs(T As tfRequest) As List(Of tmProjSecReq)
        getTFSecReqs = New List(Of tmProjSecReq)

        Dim jBody$ = ""
        jBody = JsonConvert.SerializeObject(T)

        Dim jsoN$ = getAPIData("/api/threatframework", True, jBody$)

        getTFSecReqs = JsonConvert.DeserializeObject(Of List(Of tmProjSecReq))(jsoN)
    End Function


    Public Function getTFThreats(T As tfRequest) As List(Of tmProjThreat)
        Dim TF As New List(Of tmProjThreat)

        Dim jBody$ = ""
        jBody = JsonConvert.SerializeObject(T)

        Dim jsoN$ = getAPIData("/api/threatframework", True, jBody$)

        TF = JsonConvert.DeserializeObject(Of List(Of tmProjThreat))(jsoN)

        Return TF
    End Function

    Public Function getTFAttr(T As tfRequest) As List(Of tmAttribute)
        Dim TF As New List(Of tmAttribute)

        Dim jBody$ = ""
        jBody = JsonConvert.SerializeObject(T)

        Dim jsoN$ = getAPIData("/api/threatframework", True, jBody$)

        'Console.WriteLine(jsoN)

        TF = JsonConvert.DeserializeObject(Of List(Of tmAttribute))(jsoN)

        Return TF

        '        Dim nD As JObject = JObject.Parse(jsoN)
        '        Dim nodeJSON$ = nD.SelectToken("Model").ToString
        '
        ''       Dim nodeS As List(Of tmNodeData)
        '       nodeJSON = cleanJSON(nodeJSON)
        '        nodeJSON = cleanJSONright(nodeJSON)
        '
        '        Dim SS As New JsonSerializerSettings
        '        SS.NullValueHandling = NullValueHandling.Ignore
        '
        '        nodeS = JsonConvert.DeserializeObject(Of List(Of tmNodeData))(nodeJSON, SS)
        '
        ' getProject = JsonConvert.DeserializeObject(Of tmModel)(jsoN)
        ' getProject.Nodes = nodeS


    End Function

    Public Sub buildCompObj(ByVal C As tmComponent) ' As tmComponent 'C As tmComponent, ByRef TH As List(Of tmProjThreat), ByRef SR As List(Of tmProjSecReq)) As tmComponent
        ' Assumes you are passing completed lists of TH and SR using methods above (/api/framework)
        ' This func uses an API QUERY to identify attached TH/DirectSR but only ID and NAME is returned
        ' Full list used to "attach" more complete TH/SR from lists passed ByRef
        ' Will query for Transitive SRs
        ' Recommend multi-threading this func
        ' Multi-threading full pull of SRs attached to Threats might be overkill, especially if they arent used :)

        Dim cResp As New tmCompQueryResp

        Dim jBody$ = ""
        Dim modeL$ = JsonConvert.SerializeObject(C) 'submits serialized model with escaped quotes

        Dim cReq As New tmTFQueryRequest
        With cReq
            .Model = modeL
            .LibraryId = C.LibraryId
            .EntityType = "Components"
        End With

        jBody = JsonConvert.SerializeObject(cReq)

        Dim json$ = getAPIData("/api/threatframework/query", True, jBody)

        'cResp = JsonConvert.DeserializeObject(Of tmCompQueryResp)(json)
        'would have been nice if this had worked.. cresp is a class made up of 3 lists
        'instead have to break up json/sloppy
        Dim thStr$ = Mid(json, 2, InStr(json, "],") - 1)
        json = Mid(json, InStr(json, "],") + 1)
        Dim srStr$ = Mid(json, InStr(json, ",") + 1, InStr(json, "]") - 1)
        json = Mid(json, InStr(json, "],") + 1)
        Dim attStr$ = Mid(json, InStr(json, ",") + 1, InStr(json, "]") - 1)

        Dim ndX As Integer

        With cResp
            .listThreats = JsonConvert.DeserializeObject(Of List(Of tmProjThreat))(thStr)
            .listDirectSRs = JsonConvert.DeserializeObject(Of List(Of tmProjSecReq))(srStr)
            If Len(attStr) > 3 Then
                .listAttr = JsonConvert.DeserializeObject(Of List(Of tmAttribute))(attStr)
                ' If .listAttr.Count > 1 Then
                ' saveJSONtoFile(attStr, "attrib_" + C.Id + ".json")
                'End If
            End If
            For Each T In .listAttr
                Dim ndxAT = ndxATTR(T.Id)
                If ndxAT <> -1 Then C.listAttr.Add(lib_AT(ndxAT))
            Next

            For Each T In .listThreats
                ndX = ndxTHlib(T.Id) ', lib_TH)
                C.listThreats.Add(lib_TH(ndX))
            Next
            For Each T In .listDirectSRs
                ndX = ndxSRlib(T.Id) ', lib_SR)
                If ndX <> -1 Then C.listDirectSRs.Add(lib_SR(ndX))
            Next

        End With


        If C.ComponentTypeName = "Protocols" Or C.ComponentTypeName = "Security Control" Then GoTo skipForProtocolsAndControls

        Dim possibleTransSRs As New List(Of tmProjSecReq)
        possibleTransSRs = addTransitiveSRs(C)

        For Each TSR In possibleTransSRs
            'C.listTransSRs.Add(TSR)
            If C.numLabels > 0 Then
                If numMatchingLabels(C.Labels, TSR.Labels) / C.numLabels > 0.9 Then C.listTransSRs.Add(TSR)
            End If
        Next

        If possibleTransSRs.Count Then Console.WriteLine("Label matching " + possibleTransSRs.Count.ToString + " down to " + C.listTransSRs.Count.ToString + " SRs")

        'GoTo skipForProtocolsAndControls

        For Each AT In C.listAttr
            For Each O In AT.Options
                For Each TH In O.Threats
                    Dim possibleAttrSRs As New List(Of tmProjSecReq)
                    possibleAttrSRs = addTransitiveSRsOFattr(C, O)

                    Dim ndxT As Integer = ndxTHlib(TH.Id)

                    If ndxT <> -1 Then
                        For Each TSR In lib_TH(ndxT).listLinkedSRs
                            'C.listTransSRs.Add(TSR)
                            Dim ndxS As Integer = ndxSRlib(TSR)

                            If C.numLabels > 0 Then
                                If numMatchingLabels(C.Labels, lib_SR(ndxS).Labels) / C.numLabels > 0.9 Then TH.listLinkedSRs.Add(TSR)
                            End If
                        Next
                    End If

                    '                    If possibleAttrSRs.Count Then Console.WriteLine("Label matching " + possibleAttrSRs.Count.ToString + " down to " + TH.listLinkedSRs.Count.ToString + " SRs")
                Next
            Next
            Next


skipForProtocolsAndControls:

        C.isBuilt = True

    End Sub

    Private Function addTransitiveSRs(ByVal C As tmComponent) As List(Of tmProjSecReq)
        addTransitiveSRs = New List(Of tmProjSecReq)
        If C.listThreats.Count = 0 Then Exit Function

        Dim K As Integer
        Dim L As Integer

        Dim ndX As Integer

        Dim jBody$
        Dim jSon$
        Dim tcStr$
        Dim tsrStr$
        Dim transSRs As List(Of tmProjSecReq)
        Dim T As tmProjThreat
        Dim ndxTH As Integer
        Dim cReq As tmTFQueryRequest
        Dim modeL$

        '        Dim pauseVAL As Boolean

        For K = 0 To C.listThreats.Count - 1
            T = C.listThreats(K)
            'here we get the transitive SRs
            ndxTH = ndxTHlib(T.Id) ', lib_TH)


            If lib_TH(ndxTH).isBuilt = True Then
                ' added doevents as this seems to trip// suspect lowlevel probs with isBuilt prop
                'this seems to happen a lot
                'if we already know SRs, just add them from coll and skip web request
                For Each TSRID In lib_TH(ndxTH).listLinkedSRs
                    If ndxSR(TSRID, addTransitiveSRs) <> -1 Then GoTo duphere
                    ndX = ndxSRlib(TSRID) ', lib_SR)
                    If ndX <> -1 Then addTransitiveSRs.Add(lib_SR(ndX))
duphere:
                Next
                GoTo skipTheLoad
                ' Else
            End If

            lib_TH(ndxTH).listLinkedSRs = New Collection
            ' End If

            cReq = New tmTFQueryRequest
            modeL$ = JsonConvert.SerializeObject(T) 'submits serialized model with escaped quotes

            Console.WriteLine("Compiling details for ThreatID " + T.Id.ToString)
            With cReq
                .Model = modeL
                .LibraryId = T.LibraryId
                .EntityType = "Threats"
            End With

            jBody$ = JsonConvert.SerializeObject(cReq)
            jSon$ = getAPIData("/api/threatframework/query", True, jBody)
            ' should come back with TESTCASES,SECREQ lists

            tcStr$ = Mid(jSon, 2, InStr(jSon, "],") - 1) 'test cases here if you want 'em
            jSon = Mid(jSon, InStr(jSon, "],") + 1)
            tsrStr$ = Mid(jSon, InStr(jSon, ",") + 1, InStr(jSon, "]") - 1)

            transSRs = New List(Of tmProjSecReq)
            transSRs = JsonConvert.DeserializeObject(Of List(Of tmProjSecReq))(tsrStr)

            With lib_TH(ndxTH).listLinkedSRs
                For L = 0 To transSRs.Count - 1
                    Dim TSR As tmProjSecReq = transSRs(L)
                    .Add(TSR.Id)

                    If ndxSR(TSR.Id, addTransitiveSRs) <> -1 Then GoTo duplicate
                    ndX = ndxSRlib(TSR.Id) ', lib_SR)
                    If ndX <> -1 Then addTransitiveSRs.Add(lib_SR(ndX))
duplicate:
                Next
            End With
skipTheLoad:
            lib_TH(ndxTH).isBuilt = True

        Next


    End Function

    Private Function addTransitiveSRsOFattr(ByVal C As tmComponent, ByRef O As tmOptions) As List(Of tmProjSecReq)
        addTransitiveSRsOFattr = New List(Of tmProjSecReq)
        If O.Threats.Count = 0 Then Exit Function

        Dim K As Integer
        Dim L As Integer

        Dim ndX As Integer

        Dim jBody$
        Dim jSon$
        Dim tcStr$
        Dim tsrStr$
        Dim transSRs As List(Of tmProjSecReq)
        Dim T As tmProjThreat
        Dim ndxTH As Integer
        Dim cReq As tmTFQueryRequest
        Dim modeL$

        '        Dim pauseVAL As Boolean



        For K = 0 To O.Threats.Count - 1
            T = O.Threats(K)
            'here we get the transitive SRs
            ndxTH = ndxTHlib(T.Id) ', lib_TH)


                    If lib_TH(ndxTH).isBuilt = True Then
                        ' added doevents as this seems to trip// suspect lowlevel probs with isBuilt prop
                        'this seems to happen a lot
                        'if we already know SRs, just add them from coll and skip web request
                        For Each TSRID In lib_TH(ndxTH).listLinkedSRs
                            If ndxSR(TSRID, addTransitiveSRsOFattr) <> -1 Then GoTo duphere
                            ndX = ndxSRlib(TSRID) ', lib_SR)
                    If ndX <> -1 Then addTransitiveSRsOFattr.Add(lib_sr(ndx))
duphere:
                        Next
                        GoTo skipTheLoad
                        ' Else
                    End If

                    lib_TH(ndxTH).listLinkedSRs = New Collection
                    ' End If

                    cReq = New tmTFQueryRequest
                    modeL$ = JsonConvert.SerializeObject(T) 'submits serialized model with escaped quotes

            Console.WriteLine("Compiling details for Attribute " + O.Name + " Threat " + T.Id.ToString)
            With cReq
                        .Model = modeL
                        .LibraryId = T.LibraryId
                        .EntityType = "Threats"
                    End With

                    jBody$ = JsonConvert.SerializeObject(cReq)
                    jSon$ = getAPIData("/api/threatframework/query", True, jBody)
                    ' should come back with TESTCASES,SECREQ lists

                    tcStr$ = Mid(jSon, 2, InStr(jSon, "],") - 1) 'test cases here if you want 'em
                    jSon = Mid(jSon, InStr(jSon, "],") + 1)
                    tsrStr$ = Mid(jSon, InStr(jSon, ",") + 1, InStr(jSon, "]") - 1)

                    transSRs = New List(Of tmProjSecReq)
                    transSRs = JsonConvert.DeserializeObject(Of List(Of tmProjSecReq))(tsrStr)

                    With lib_TH(ndxTH).listLinkedSRs
                        For L = 0 To transSRs.Count - 1
                            Dim TSR As tmProjSecReq = transSRs(L)
                            .Add(TSR.Id)

                            If ndxSR(TSR.Id, addTransitiveSRsOFattr) <> -1 Then GoTo duplicate
                    'ndX = ndxSRlib(TSR.Id) ', lib_SR)
                    addTransitiveSRsOFattr.Add(TSR)
duplicate:
                        Next
                    End With
skipTheLoad:
                    lib_TH(ndxTH).isBuilt = True

                Next



    End Function

    Public Sub defineTransSRs(ByVal threatID As Long)
        Dim jBody$
        Dim jSon$
        Dim tcStr$
        Dim tsrStr$
        Dim transSRs As List(Of tmProjSecReq)
        Dim T As tmProjThreat
        Dim ndxTH As Integer
        Dim cReq As tmTFQueryRequest
        Dim modeL$

        '        Dim pauseVAL As Boolean

        'here we get the transitive SRs
        ndxTH = ndxTHlib(threatID)
        T = lib_TH(ndxTH)

        cReq = New tmTFQueryRequest
        modeL$ = JsonConvert.SerializeObject(T) 'submits serialized model with escaped quotes

        With cReq
            .Model = modeL
            .LibraryId = T.LibraryId
            .EntityType = "Threats"
        End With

        jBody$ = JsonConvert.SerializeObject(cReq)
        jSon$ = getAPIData("/api/threatframework/query", True, jBody)
        ' should come back with TESTCASES,SECREQ lists

        tcStr$ = Mid(jSon, 2, InStr(jSon, "],") - 1) 'test cases here if you want 'em
        jSon = Mid(jSon, InStr(jSon, "],") + 1)
        tsrStr$ = Mid(jSon, InStr(jSon, ",") + 1, InStr(jSon, "]") - 1)

        transSRs = New List(Of tmProjSecReq)
        transSRs = JsonConvert.DeserializeObject(Of List(Of tmProjSecReq))(tsrStr)

        T.listLinkedSRs = New Collection

        For Each TSR In transSRs
            If grpNDX(T.listLinkedSRs, TSR.Id) <> -1 Then GoTo duplicate
            T.listLinkedSRs.Add(TSR.Id)
duplicate:
        Next
skipTheLoad:

        'RaiseEvent threatBuilt(T)
    End Sub

    Public Function numMatchingLabels(ByVal LofParent$, ByVal LofChild$) As Integer
        numMatchingLabels = 0
        Dim PL As New Collection
        PL = CSVtoCOLL(LofParent)
        Dim CL As New Collection
        CL = CSVtoCOLL(LofChild)

        For Each C In CL
            If grpNDX(PL, C) Then numMatchingLabels += 1
        Next
    End Function

    Public Function getMatchingLabels(ByVal LofParent$, ByVal LofChild$, Optional ByVal nonMatching As Boolean = False) As Collection
        getMatchingLabels = New Collection '  = 0
        Dim PL As New Collection
        PL = CSVtoCOLL(LofParent)
        Dim CL As New Collection
        CL = CSVtoCOLL(LofChild)

        For Each C In CL
            If grpNDX(PL, C) Then
                If nonMatching = False Then getMatchingLabels.Add(C)
            End If
        Next

        If nonMatching = True Then
            Dim unSelected As New Collection
            For Each PLBL In PL
                If grpNDX(getMatchingLabels, PLBL) = 0 Then
                    unSelected.Add(PLBL)
                End If
            Next
            Return unSelected
        End If

    End Function

    Public Class tmTFQueryRequest
        Public Model$
        Public LibraryId As Integer
        Public EntityType$
    End Class
    Public Function returnSRLZcomponent(C As tmComponent) As String
        Return JsonConvert.SerializeObject(C).ToString

    End Function






    Public Function loadAllGroups(ByRef T As TM_Client, Optional ByVal louD As Boolean = False) As List(Of tmGroups)

        Dim GRP As List(Of tmGroups) = T.getGroups

        For Each G In GRP
            G.AllProjInfo = T.getProjectsOfGroup(G)
            If louD Then Console.WriteLine(G.Name + ": " + G.AllProjInfo.Count.ToString + " models")
            For Each P In G.AllProjInfo
                P.Model = T.getProject(P.Id)
                If louD Then Console.WriteLine(vbCrLf)
                If louD Then Console.WriteLine(col5CLI("Proj/Comp Name", "#/TYPE COMP", "# THR", "# SR"))
                If louD Then Console.WriteLine(col5CLI("--------------", "-----------", "-----", "----"))
                With P.Model
                    T.addThreats(P.Model, P.Id) 'add in details of threats not inside Diagram API (eg Protocols)
                    If louD Then Console.WriteLine(col5CLI(P.Name + " [Id " + P.Id.ToString + "]", .Nodes.Count.ToString, P.Model.modelNumThreats.ToString, P.Model.modelNumThreats(True).ToString))
                    '     Console.WriteLine(col5CLI("   ", "TYPE", "# THR", "# SR"))
                    For Each N In .Nodes
                        If N.category = "Collection" Then
                            If louD Then Console.WriteLine(col5CLI(" >" + N.FullName, "Collection", "", ""))
                            GoTo doneHere
                        End If
                        If N.category = "Project" Then
                            N.ComponentName = N.FullName
                            N.ComponentTypeName = "Nested Model"
                            '                            Console.WriteLine(col5CLI(" >" + N.FullName, "Nested Model", "", ""))
                            'GoTo doneHere
                        End If
                        If louD Then Console.WriteLine(col5CLI(" >" + N.ComponentName, N.ComponentTypeName, N.listThreats.Count.ToString, N.listSecurityRequirements.Count.ToString))
                        '                        Console.WriteLine("------------- " + N.Name + "-Threats:" + N.listThreats.Count.ToString + "-SecRqrmnts:" + N.listSecurityRequirements.Count.ToString)
doneHere:
                    Next
                End With
                If P.Model.Nodes.Count Then
                    If louD Then Console.WriteLine(col5CLI("----------------------------------", "------------------------", "------", "------"))
                End If
            Next

            If louD Then Console.WriteLine(vbCrLf)
        Next

        Return GRP
    End Function

    Public Sub addThreats(ByRef tmMOD As tmModel, ByVal projID As Long)
        ' not all threats are included in COMPONENTS API
        ' to capture all of tmModel and Nodes, pull in Threats API for Project
        ' this routine bridges the gap between info provided in 2 APIs and puts 
        ' as much into tmModel as it can

        Dim jsoN$ = getAPIData("/api/project/" + projID.ToString + "/threats?openStatusOnly=false")
        Dim tNodes As New List(Of tmTThreat)

        tNodes = JsonConvert.DeserializeObject(Of List(Of tmTThreat))(jsoN)

        For Each T In tNodes
            If T.SourceType = "Link" Then
                Dim mNode As Integer
                ' first check parent node.. if parent is a threat model (category=Project)
                ' then do not add the link
                mNode = tmMOD.ndxOFnode(T.SourceId)
                If mNode <> -1 Then
                    'unless id > 300000 and subsequent code creates artificial node
                    'this should be a project/nested model.. do not add these links
                    If tmMOD.Nodes(mNode).category = "Project" Then GoTo skipThose
                End If

                mNode = tmMOD.ndxOFnode(300000 + T.SourceId)
                If mNode = -1 Then
                    Call addNode(tmMOD, T)
                End If
                mNode = tmMOD.ndxOFnode(300000 + T.SourceId)

                With tmMOD.Nodes(mNode)
                    Dim newT As New tmProjThreat
                    newT.Name = T.ThreatName
                    newT.StatusName = T.StatusName
                    newT.Id = T.ThreatId
                    newT.Description = T.Description
                    ' notes is missing!
                    newT.RiskName = T.ActualRiskName
                    .listThreats.Add(newT)
                    .threatCount += 1
                End With
            End If
            'If Mid(T.ThreatName, 1, 4) = "11111111CVE-" Then
            ' have to manually add in CPE-related threats to node collection
            ' correction
            ' CVEs are added as Threats to nodes in the DIAGRAM API, but the "numThreats" property does not
            ' include CVEs as threats. Use node.listThreats.nodecount in lieu of node.numThreats API property
            '
            ' protocols of Nested Models are included in the counts of threats as components


skipThose:
        Next

        'getGroups = JsonConvert.DeserializeObject(Of List(Of tmGroups))(jsoN)
    End Sub

    Public Function ndxVPC(ByVal vpcID As Long, ByRef VPCs As List(Of tmVPC)) As Integer
        'finds object (for now, the node of a model) matching idOFnode
        Dim K As Long = 0

        For Each N In VPCs
            If N.Id = vpcID Then
                Return K
            End If
            K += 1
        Next

        Return -1
    End Function

    Public Function ndxATTR(ID As Long) As Integer
        ndxATTR = -1

        Dim ndX As Integer = 0
        For Each P In lib_AT
            If P.Id = ID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function


    Public Function ndxTHlib(ID As Long) As Integer
        ndxTHlib = -1

        Dim ndX As Integer = 0
        For Each P In lib_TH
            If P.Id = ID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function

    Public Function ndxSRlib(ID As Long) As Integer
        ndxSRlib = -1

        Dim ndX As Integer = 0
        For Each P In lib_SR
            If P.Id = ID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function


    Public Function ndxLib(ID As Long) As Integer
        ndxLib = -1

        Dim ndX As Integer = 0
        For Each P In librarieS
            If P.Id = ID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function
    Public Function ndxComp(ID As Long) As Integer ', ByRef alL As List(Of tmComponent)) As Integer
        ndxComp = -1

        'had to change this as passing byref from multi-threaded main causing issues

        Dim ndX As Integer = 0
        For Each P In lib_Comps
            If P.Id = ID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function

    Public Function ndxCompbyName(name$) As Integer ', ByRef alL As List(Of tmComponent)) As Integer
        ndxCompbyName = -1

        'had to change this as passing byref from multi-threaded main causing issues

        Dim ndX As Integer = 0
        For Each P In lib_Comps
            If LCase(P.Name) = LCase(name) Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function

    Public Function ndxSRbyName(name$) As Integer ', ByRef alL As List(Of tmComponent)) As Integer
        ndxSRbyName = -1

        'had to change this as passing byref from multi-threaded main causing issues

        Dim ndX As Integer = 0
        For Each P In lib_SR
            If LCase(P.Name) = LCase(name) Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function

    Public Function ndxSR(ID As Long, ByRef alL As List(Of tmProjSecReq)) As Integer
        ndxSR = -1

        Dim ndX As Integer = 0
        For Each P In alL
            If P.Id = ID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function



    Public Sub addNode(ByRef tmMOD As tmModel, ByRef T As tmTThreat)
        ' create node inside tmMOD based on threat T (from Threats API)
        'Dim tmMOD As tmModel = tmPROJ.Model

        Dim newNode As tmNodeData = New tmNodeData

        If T.SourceType = "Link" Then
            newNode.Id = T.SourceId + 300000
            newNode.category = "Component"
            newNode.Name = T.SourceDisplayName
            newNode.type = "Link"
            newNode.ComponentName = T.SourceName
            newNode.ComponentTypeName = "Protocol"

            tmMOD.Nodes.Add(newNode)
            Exit Sub
        End If


    End Sub

    Public Function getProject(projID As Long) As tmModel
        getProject = New tmModel

        Dim jsoN$ = getAPIData("/api/diagram/" + projID.ToString + "/components")

        Dim nD As JObject = JObject.Parse(jsoN)
        Dim nodeJSON$ = nD.SelectToken("Model").ToString

        Dim nodeS As List(Of tmNodeData)
        nodeJSON = cleanJSON(nodeJSON)
        nodeJSON = cleanJSONright(nodeJSON)

        Dim SS As New JsonSerializerSettings
        SS.NullValueHandling = NullValueHandling.Ignore

        nodeS = JsonConvert.DeserializeObject(Of List(Of tmNodeData))(nodeJSON, SS)

        getProject = JsonConvert.DeserializeObject(Of tmModel)(jsoN)
        getProject.Nodes = nodeS
    End Function

End Class

Public Class tfRequest
    'obj used to request Threats, SRs, Components
    'unpublished API
    '    "EntityType""SecurityRequirements","LibraryId":"0","ShowHidden":false
    Public EntityType$
    Public LibraryId As Integer '0 = ALL
    Public ShowHidden As Boolean
End Class

Public Class tmAWSacc
    Public Id As Long
    Public Name$

End Class
Public Class tmVPC
    Public Id As Long
    Public VPCId$
    Public ThirdpartyInfoId As Long
    Public ProjectId? As Integer
    Public ProjectName$
    Public Version$
    Public VPCTags$
    Public IsSuccess As Boolean
    Public Notes$
    Public Labels$
    Public RiskId As Integer
    Public IsInternal As Boolean
    Public LastSync$
    Public Region$
    Public GroupPermissions As List(Of String)
    Public UserPermissions As List(Of String)
    Public [Type] As String


End Class
Public Class tmGroups
        Public Id As Long
        Public Name$
        Public isSystem As Boolean
        Public ParentGroupId As Integer
        Public ParentGroupName$
        Public Department$
        Public DepartmentId$
        Public Guid As System.Guid
        Public Projects$
        Public GroupUsers$
        Public AllProjInfo As List(Of tmProjInfo)
    End Class

Public Class tmLabels
    Public Id As Long
    Public Name$
    Public IsSystem As Boolean
    Public Attributes$
End Class

Public Class tmProjInfo
    Public Id As Long
    Public Name$
    Public Version$
    'Public Guid As System.Guid
    Public isInternal As Boolean
    Public RiskId As Integer
    Public RiskName$
    Public CreatedByName$
    Public LastModifiedByName$
    'Public RiskName As Integer
    Public [Type] As String
    Public CreatedByUserEmail$
    Public Model As tmModel
    'Public nodesFromThreats As Integer 'beginning at 300k, nodes from threats like HTTPS
End Class

Public Class tmProjThreat
    ' these threats come from the DIAGRAM API as part of nodeArray
    Public Id As Long
    'Public Guid as system.guid 'nulls
    Public Name$
    Public Description$
    Public RiskId As Integer
    Public RiskName$
    Public LibraryId As Integer
    Public Labels$
    Public Reference$
    Public Automated As Boolean
    'public ThreatAttributes$ 'nulls
    Public StatusName$
    Public IsHidden As Boolean
    Public CompanyId As Integer
    'Public SharingType$
    'Public ReadOnly As Boolean
    Public isDefault As Boolean
    Public DateCreated$
    Public LastUpdated$
    Public DepartmentId As Integer
    Public DepartmentName$
    'public Version$ 
    Public IsSystemDepartment As Boolean

    Public isBuilt As Boolean
    Public listLinkedSRs As Collection

    Public ReadOnly Property ThrID
        Get
            Return Id
        End Get
    End Property
    Public ReadOnly Property ThrName
        Get
            Return Name
        End Get
    End Property
    Public ReadOnly Property ThRisk
        Get
            Return RiskName
        End Get
    End Property
    Public Sub New()
        listLinkedSRs = New Collection
    End Sub
End Class

Public Class tmTThreat
    Public Id As Long

    Public ThreatName$
    Public SourceName$
    Public SourceDisplayName$
    Public ThreatId As Integer
    Public ActualRiskName$
    Public StatusName$
    Public Description$
    Public Notes$
    Public SourceId As Long
    Public SourceType$

End Class
Public Class tmLibrary
    Public Id As Long
    Public Name$
    Public Description$
    Public CompanyId As Integer
    Public Guid As System.Guid
    Public SharingType$
    Public [Readonly] As Boolean
    Public isDefault As Boolean
    Public DateCreated$
    Public LastUpdated$
    Public DepartmentId As Integer
    Public DepartmentName$
    Public Labels$
    Public Version$
    Public IsSystemDepartment As Boolean

End Class
Public Class tmCompQueryResp
    Public listThreats As List(Of tmProjThreat)
    Public listDirectSRs As List(Of tmProjSecReq)
    'Public listTransSRs As List(Of tmProjSecReq)
    Public listAttr As List(Of tmAttribute)
End Class
Public Class tmAttribute
    Public Id As Long
    Public Name$
    Public LibraryId As Integer
    Public Options() As tmOptions ' As List(Of tmOptions)

    Public Sub New()
        '        Options = New List(Of tmOptions)
    End Sub
    '    "Id" 1671,
    '    "Guid": "88404d54-fe0a-4a43-a2f8-a0238de44f4e",
    '    "Name": "Are there inputs with potential alternate encoding that could bypass input sanitization?",
    '    "Labels": null,
    '    "Description": "",
    '    "LibraryId": 66,
    '    "isSelected": false,
    '    "IsHidden": false,
    '    "IsOptional": true,
    '    "AttributeType": "SingleSelect",
    '    "PropertyTypeId": 0,
    '    "AttributeAnswer": null,
    '    "Options": [
    '      {
    '        "Id": 1827,
    '        "Name": "Yes",
    '        "IsDefault": false,
    '        "Threats": [
    '          {
    '            "Id": 343,
    '            "Guid": null,
    '            "Name": "Using Slashes and URL Encoding Combined to Bypass Validation Logic",
    '            "Description": null,

End Class
Public Class tmOptions
    Public Id As Integer
    Public Name$
    Public isDefault As Boolean
    Public Threats() As tmProjThreat

    Public Sub New()
        '        Threats = New List(Of tmProjThreat)
    End Sub
End Class
Public Class tmComponent
    Public Id As Long
    Public Name$
    Public Description$
    Public ImagePath$
    Public Labels$
    Public Version$
    Public LibraryId As Integer
    Public ComponentTypeId As Integer
    Public ComponentTypeName$
    Public Color$
    Public Guid As System.Guid
    Public IsHidden As Boolean
    Public DiagralElementId As Integer
    Public ResourceTypeValue$
    'Public Attributes$

    Public listThreats As List(Of tmProjThreat)
    Public listDirectSRs As List(Of tmProjSecReq)
    Public listTransSRs As List(Of tmProjSecReq)
    Public listAttr As List(Of tmAttribute)
    Public isBuilt As Boolean
    Public Function numLabels() As Integer
        If Len(Labels) = 0 Then
            Return 0
        Else
            Return numCHR(Labels, ",") + 1
        End If
    End Function

    Public ReadOnly Property CompID() As Long
        Get
            Return Id
        End Get
    End Property
    Public ReadOnly Property CompName() As String
        Get
            Return Name
        End Get
    End Property
    Public ReadOnly Property TypeName() As String
        Get
            Return ComponentTypeName
        End Get
    End Property
    Public ReadOnly Property NumTH() As Integer
        Get
            Return listThreats.Count
        End Get
    End Property

    Public ReadOnly Property NumSR() As Integer
        Get
            Return listDirectSRs.Count + listTransSRs.Count
        End Get
    End Property


    Public Sub New()
        listThreats = New List(Of tmProjThreat)
        listDirectSRs = New List(Of tmProjSecReq)
        listTransSRs = New List(Of tmProjSecReq)
        listAttr = New List(Of tmAttribute)
        isBuilt = False
    End Sub

End Class


Public Class tmProjSecReq
    Public Id As Long
    Public Name$
    Public Description$
    Public RiskId As Integer
    Public IsCompensatingControl As Boolean
    Public RiskName$
    Public Labels$
    Public LibraryId As Integer
    'Public Guid as System.Guid
    Public StatusName$
    Public SourceName$
    Public IsHidden As Boolean
End Class

Public Class tmNodeData
        Public key As Single
        Public category$
        Public Name$
        Public [type] As String
        Public ImageURI$
        Public NodeId As Integer
        Public ComponentId As Integer
        Public group As Single
        Public Location$
        Public color$
        Public BorderColor$
        Public BackgroundColor$
        Public TitleColor$
        Public TitleBackgroundColor$
        Public isGroup As Boolean
        Public isGraphExpanded As Boolean
        Public FullName$
        Public cpeid$ ' As Integer
        Public networkComponentId As Integer
        Public BorderThickness As Decimal
        Public AllowMove As Boolean
        Public LayoutWidth As Single
        Public LayoutHeight As Single
        Public componentProperties$
        Public listThreats As List(Of tmProjThreat)
        Public listSecurityRequirements As List(Of tmProjSecReq)
    Public ComponentTypeName$
    Public ComponentName$
    Public Notes$
    Public Id As Long
    Public threatCount As Integer

    Public Sub New()
        Me.listSecurityRequirements = New List(Of tmProjSecReq)
        Me.listThreats = New List(Of tmProjThreat)

    End Sub
End Class

Public Class TF_Threat
    '    {\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":85,\"RiskId\":1,\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"SampleThreat444444\",\"Labels\":\"AppSec and InfraSec,DevOps\",\"LibrayId\":66,\"Description\":\"<p>Example123</p>\",\"IsCopy\":false}
    Public ImagePath$
    Public ComponentTypeId As Integer
    Public RiskId As Integer
    Public CodeTypeId As Integer
    Public DataClassificationId As Integer
    Public Name$
    Public Labels$
    Public LibrayId As Integer
    Public Description$
    Public IsCopy As Boolean
End Class
Public Class tmModel
    ' model is built from DIAGRAM API
    ' includes all threats and SRs except
    '       protocol threats
    '       CPE CVEs
    '       Nested Models
    ' these require lookup into Threats API
    ' Can correlate CPE CVEs to Component of SourceId
    ' Pull Nested Model Threats in and also store to SourceId

    Public Id As Long
    Public Name$
    Public IsAppliedForApproval As Boolean
    Public Guid As System.Guid
    Public Nodes As List(Of tmNodeData)

    Public Function modelNumThreats(Optional ByVal SecReqInstead As Boolean = False) As Integer
        Dim nuM As Integer = 0
        For Each N In Me.Nodes
            If SecReqInstead Then nuM += N.listSecurityRequirements.Count Else nuM += N.listThreats.Count
        Next
        Return nuM
    End Function

    Public Function ndxOFnode(ByVal idOFnode As Long) As Integer
        'finds object (for now, the node of a model) matching idOFnode
        Dim K As Long = 0

        For Each N In Me.Nodes
            If N.Id = idOFnode Then
                Return K
            End If
            K += 1
        Next

        Return -1
    End Function

End Class

