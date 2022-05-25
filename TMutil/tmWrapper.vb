Imports RestSharp
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
    Public componentTypes As List(Of tmMiscTrigger)
    Public roleS As List(Of tmMiscTrigger)
    Public dataElements As List(Of tmMiscTrigger)


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

    Private Function tfAdd_GetAPIData(ByVal urI$, addJSONbody$)

        ' DO NOT NEED THIS FUNCTION...

        tfAdd_GetAPIData = ""
        Dim client = New RestClient(tmFQDN + urI)
        Dim request As RestRequest
        request = New RestRequest(Method.POST)

        Dim response As IRestResponse
        client.Timeout = -1

        Dim c$ = Chr(34)

        'var request = New RestRequest(Method.POST);
        request.AddHeader("sec-ch-ua", c + "Google Chrome" + c + ";v=" + c + "95" + c + ", " + c + "Chromium" + c + ";v=" + c + "95" + c + ", " + c + ";Not A Brand" + c + ";v=" + c + "99" + c)
        request.AddHeader("sec-ch-ua-mobile", "?0")
        request.AddHeader("Authorization", "Bearer " + slToken)
        request.AddHeader("Content-Type", "multipart/form-data") '; boundary=----WebKitFormBoundary1bCpiyr5KkmiBIAJ")
        request.AddHeader("Accept", "application/json, text/plain, */*")
        client.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36"
        request.AddHeader("sec-ch-ua-platform", c + "macOS" + c)
        request.AlwaysMultipartFormData = True
        request.AddParameter("data", addJSONbody)

        response = client.Execute(request)

        Dim K As Integer
        K = 1

    End Function
    Private Function getAPIData(ByVal urI$, Optional ByVal usePOST As Boolean = False, Optional ByVal addJSONbody$ = "", Optional ByVal addingComp As Boolean = False, Optional ByVal addFileN$ = "") As String
        getAPIData = ""
        If isConnected = False Then Exit Function

        Dim client = New RestClient(tmFQDN + urI)
        Dim request As RestRequest
        If usePOST = False Then request = New RestRequest(Method.GET) Else request = New RestRequest(Method.POST)

        Dim response As IRestResponse

        request.AddHeader("Authorization", "Bearer " + slToken)
        request.AddHeader("Accept", "application/json")

        If Len(addJSONbody) Then
            If addingComp = False Then
                request.AddHeader("Content-Type", "application/json")
            Else
                request.AddHeader("Content-Type", "multipart/form-data") '; boundary=----WebKitFormBoundary1bCpiyr5KkmiBIAJ")
            End If
            request.AddHeader("Accept-Encoding", "gzip, deflate, br")

            'request.AddHeader("Accept-Language", "zh")
            'adding this (Lenovo Chinese) messes up API calls for COmponentTypes and Attributes (prob more)

            request.AddHeader("Connection", "keep-alive")
            If addingComp = False Then
                request.AddParameter("application/json", addJSONbody, ParameterType.RequestBody)
            Else
                request.AddParameter("data", addJSONbody, ParameterType.RequestBody)
                request.AlwaysMultipartFormData = True
            End If
        End If

        If Len(addFileN) Then
            request.AddFile("File", addFileN)
        End If

        response = client.Execute(request)

        If addingComp Then
            Return response.Content
        End If

        Dim O As JObject = JObject.Parse(response.Content)

        If IsNothing(O.SelectToken("IsSuccess")) = True Then
            Console.WriteLine("API Request Rejected:  " + urI)
            getAPIData = ""
            Exit Function
        End If

        '        If Len(addFileN) Then
        '            If O.SelectToken("IsSuccess").ToString = "true" Then
        '                Return O.SelectToken("IsSuccess").ToString
        '            Else
        '                Return response.Content
        '            End If
        '        End If

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

    Public Function getEntityMisc(T As tfRequest) As List(Of tmMiscTrigger)
        Dim TF As New List(Of tmMiscTrigger)

        Dim jBody$ = ""
        jBody = JsonConvert.SerializeObject(T)

        Dim jsoN$ = getAPIData("/api/threatframework", True, jBody$)

        TF = JsonConvert.DeserializeObject(Of List(Of tmMiscTrigger))(jsoN)

        Return TF
    End Function

    Public Function updateThreatStat(T As threatStatusUpdate) As String
        updateThreatStat = ""

        Dim jBody$ = ""
        jBody = JsonConvert.SerializeObject(T)

        Return jBody

        Exit Function

        Dim jsoN$ = getAPIData("/api/threatframework", True, jBody$)

        ' TF = JsonConvert.DeserializeObject(Of List(Of tmMiscTrigger))(jsoN)

        ' Return TF
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
    Public Function getTemplates() As List(Of tmTemplate)
        getTemplates = New List(Of tmTemplate)
        Dim jsoN$ = getAPIData("/api/templates") '/" + tempID.ToString)

        getTemplates = JsonConvert.DeserializeObject(Of List(Of tmTemplate))(jsoN)

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


    Public Function getCompFrameworks() As List(Of complianceDetails)
        Dim TF As New List(Of complianceDetails)

        Dim jsoN$ = getAPIData("/api/scf/complianceframeworks/false")

        TF = JsonConvert.DeserializeObject(Of List(Of complianceDetails))(jsoN)

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

    Public Sub removeThreatFromComponent(C As tmComponent, threatID As Integer)
        '{"propertyIds":[101],"propertyEntityType":"Threats","sourceEntityIds":[449],"LibraryId":1,"EntityType":"Components","IsAddition":false}
        Dim P As New entityUpdate
        With P
            .propertyIds.Add(threatID)
            .propertyEntityType = "Threats"
            .sourceEntityIds.Add(C.Id)
            .LibraryId = C.LibraryId
            .EntityType = "Components"
            .IsAddition = False
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/updateproperty", True, jBody)


    End Sub
    Public Sub removeAttributeFromComponent(C As tmComponent, attrID As Integer)
        '{"propertyIds":[1667],"propertyEntityType":"Attributes","sourceEntityIds":[3301],"LibraryId":66,"EntityType":"Components","IsAddition":false}
        Dim P As New entityUpdate
        With P
            .propertyIds.Add(attrID)
            .propertyEntityType = "Attributes"
            .sourceEntityIds.Add(C.Id)
            .LibraryId = C.LibraryId
            .EntityType = "Components"
            .IsAddition = False
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/updateproperty", True, jBody)

    End Sub

    Public Sub addSRtoThreat(T As tmProjThreat, srID As Integer, Optional ByVal removeInstead As Boolean = False)
        '{"propertyIds"[116],"propertyEntityType":"SecurityRequirements","sourceEntityIds":[993],"LibraryId":1,"EntityType":"Threats","IsAddition":false}

        Dim P As New entityUpdate
        With P
            .propertyIds.Add(srID)
            .propertyEntityType = "SecurityRequirements"
            .sourceEntityIds.Add(T.Id)
            .LibraryId = T.LibraryId
            .EntityType = "Threats"
            If removeInstead Then .IsAddition = False Else .IsAddition = True
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/updateproperty", True, jBody)

    End Sub

    Public Sub addAttributeToComponent(C As tmComponent, attrID As Integer)
        '{"propertyIds":[1667],"propertyEntityType":"Attributes","sourceEntityIds":[3301],"LibraryId":66,"EntityType":"Components","IsAddition":false}
        Dim P As New entityUpdateBID
        With P
            .propertyIds.Add(attrID)
            .propertyEntityType = "Attributes"
            .sourceEntityIds.Add(C.Id)
            .LibraryId = C.LibraryId
            .EntityType = "Components"
            .IsAddition = True
            .BackendId = 1
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/updateproperty", True, jBody)

    End Sub

    Public Sub addThreatToComponent(C As tmComponent, threatID As Integer)
        '{"propertyIds":[1503],"propertyEntityType":"Threats","sourceEntityIds":[3301],"LibraryId":66,"EntityType":"Components","IsAddition":true,"BackendId":1}
        Dim P As New entityUpdateBID
        With P
            .propertyIds.Add(threatID)
            .propertyEntityType = "Threats"
            .sourceEntityIds.Add(C.Id)
            .LibraryId = C.LibraryId
            .EntityType = "Components"
            .IsAddition = True
            .BackendId = 1
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/updateproperty", True, jBody)


    End Sub

    Public Function getUsers(deptID As Integer) As List(Of tmUser)
        getUsers = New List(Of tmUser)
        Dim jbody$ = "[" + deptID.ToString + "]"
        Dim json$ = getAPIData("/api/department/users", True, jbody)
        getUsers = JsonConvert.DeserializeObject(Of List(Of tmUser))(json)
    End Function


    Public Function getDepartments() As List(Of tmDept)
        getDepartments = New List(Of tmDept)
        Dim json$ = getAPIData("/api/departments")
        getDepartments = JsonConvert.DeserializeObject(Of List(Of tmDept))(json)
    End Function




    Public Sub addThreatToAttribute(A As tmAttribute, threatID As Integer)
        '{"propertyIds":[1503],"propertyEntityType":"Threats","sourceEntityIds":[3301],"LibraryId":66,"EntityType":"Components","IsAddition":true,"BackendId":1}
        Dim P As New entityUpdateBID
        With P
            .propertyIds.Add(threatID)
            .propertyEntityType = "Threats"
            .sourceEntityIds.Add(A.Id)
            .LibraryId = A.LibraryId
            .EntityType = "Attributes"
            .IsAddition = True
            '.BackendId = 1
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        '        Console.WriteLine(jBody)
        Dim json$ = getAPIData("/api/threatframework/updateproperty", True, jBody)

    End Sub

    Public Function createKISSmodelForImport(modelName$, Optional ByVal riskId As Integer = 1, Optional ByVal versioN As Integer = 1, Optional ByVal labelS$ = "", Optional ByVal modelType$ = "Others", Optional ByVal importType$ = "Kis") As String
        '{"Id":0,
        '"Name""test_meth_inst2",
        '"RiskId":1,"Labels":"",
        '"Version":"1","IsValidFile":false,
        '"Type":"AWSCloudApplication",
        '"CreatedThrough":"Blank","UserPermissions":[],
        '"GroupPermissions":[]}
        Dim newM As kisCreateModel_setProject = New kisCreateModel_setProject

        createKISSmodelForImport = ""

        With newM
            .Id = 0
            .Name = modelName
            .RiskId = riskId
            .Version = versioN
            .Labels = labelS
            .Type = modelType
            .CreatedThrough = "Blank"
            .UserPermissions = New List(Of String)
            .GroupPermissions = New List(Of String)
        End With

        Dim jBody$ = JsonConvert.SerializeObject(newM)
        Dim jSon$ = getAPIData("/api/project/create", True, jBody)



        Return jSon
    End Function

    Public Function importKISSmodel(fileN$, projNum As Integer, Optional ByVal integrationType$ = "Kis") As String
        importKISSmodel = ""



        Dim jSon$ = getAPIData("/api/import/" + projNum.ToString + "/" + integrationType, True,,, fileN)

        '    Dim stopHere As Integer = 1

        If Mid(jSon, 1, 5) = "ERROR" Then
            Return jSon
        End If

        Return "Action Completed"
    End Function

    Public Function addEditSR(SR As tmProjSecReq) As String
        addEditSR = ""
        Dim P As New updateEntityObject
        With P
            .LibraryId = SR.LibraryId
            .EntityType = "SecurityRequirements"
            .Model = JsonConvert.SerializeObject(SR)
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/master/addedit", True, jBody)
        Return json
    End Function

    Public Function addEditTH(TH As tmProjThreat) As String
        addEditTH = ""
        Dim P As New updateEntityObject
        With P
            .LibraryId = TH.LibraryId
            .EntityType = "Threats"
            .Model = JsonConvert.SerializeObject(TH)
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/master/addedit", True, jBody)
        Return json
    End Function
    Public Function addEditATTR(AT As tmAttribute) As String
        addEditATTR = ""
        Dim P As New updateEntityObject

        Dim A As New tmAttrCreate
        '{"LibraryId":0,"EntityType":"Attributes","Model":"{\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":3,\"RiskId\":1,\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"New ATTR\",\"Labels\":\"ThreatModeler\",\"LibrayId\":0,\"Description\":\"Some ATTR Desc\",\"IsCopy\":false}"}
        With A
            .ImagePath = "/ComponentImage/DefaultComponent.jpg"
            .ComponentTypeId = 3
            .RiskId = 1
            .CodeTypeId = 1
            .DataClassificationId = 1
            .Name = AT.Name
            .Labels = "ThreatModeler"
            .LibrayId = AT.LibraryId
            .Description = AT.Name
            .IsCopy = False
        End With

        With P
            .LibraryId = AT.LibraryId
            .EntityType = "Attributes"
            .Model = JsonConvert.SerializeObject(A)
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/master/addedit", True, jBody)
        Return json
    End Function

    Public Function addSR(SR As tmProjSecReq) As Boolean
        addSR = False

        Dim P As New updateEntityObject

        '"{\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":85,\"RiskId\":1,\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"NewSR\",\"Labels\":\"ThreatModeler\",\"LibrayId\":0,\"Description\":\"SR Desc\",\"IsCopy\":false}"}

        Dim S As New addSRClass

        With S
            .ImagePath = "/ComponentImage/DefaultComponent.jpg"
            .ComponentTypeId = 85
            .RiskId = 1
            .CodeTypeId = 1
            .DataClassificationId = 1
            .Name = SR.Name
            .Labels = SR.Labels
            .LibrayId = SR.LibraryId
            .Description = SR.Description
            .IsCopy = False
        End With
        With P
            .LibraryId = SR.LibraryId
            .EntityType = "SecurityRequirements"
            .Model = JsonConvert.SerializeObject(S)
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/master/addedit", True, jBody)

        Dim K As Integer
        K = 1
        If Val(json) Then addSR = True

    End Function

    Public Function addTH(SR As tmProjThreat) As Boolean
        addTH = False

        Dim P As New updateEntityObject

        '"{\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":85,\"RiskId\":1,\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"NewSR\",\"Labels\":\"ThreatModeler\",\"LibrayId\":0,\"Description\":\"SR Desc\",\"IsCopy\":false}"}

        Dim S As New addTHClass

        With S
            .ImagePath = "/ComponentImage/DefaultComponent.jpg"
            .ComponentTypeId = 85
            .RiskId = 1
            .CodeTypeId = 1
            .DataClassificationId = 1
            .Name = SR.Name
            .Labels = SR.Labels
            .LibrayId = SR.LibraryId
            .Description = SR.Description
            .IsCopy = False
        End With
        With P
            .LibraryId = SR.LibraryId
            .EntityType = "Threats"
            .Model = JsonConvert.SerializeObject(S)
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/master/addedit", True, jBody)

        Dim K As Integer
        K = 1
        If Val(json) Then addTH = True

    End Function
    Public Sub addEditThreat(T As tmProjThreat)
        Dim P As New updateEntityObject
        With P
            .LibraryId = T.LibraryId
            .EntityType = "SecurityRequirements"
            .Model = JsonConvert.SerializeObject(T)
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/master/addedit", True, jBody)

    End Sub

    Public Function hideItem(C As Object, Optional unHide As Boolean = False) As Boolean
        hideItem = False

        Dim jBody$ = ""
        Dim jSon$ = ""
        Dim P As New addCompClass
        With P
            .LibraryId = C.LibraryId
            '            .EntityType = "Components"
            '            .Model = JsonConvert.SerializeObject(COMP)
            .ActionType = "TF_ENTITY_HIDDEN"
            If unHide Then C.ishidden = False Else C.ishidden = True
        End With

        Dim TH As editTHClass = New editTHClass 'move this if rename works without deleting SRs

        Dim editJBODY$ = ""

        Select Case entityType(C)
            Case "Components"
                Dim COMP As New tmComponent
                COMP = C
                P.EntityType = "Components"

                With P
                    .EntityType = "Components"
                    .Model = JsonConvert.SerializeObject(COMP)
                End With

            Case "SecurityRequirements"
                Dim COMP As New tmProjSecReq
                COMP = C
                P.EntityType = "SecurityRequirements"

                With P
                    .EntityType = "SecurityRequirements"
                    .Model = JsonConvert.SerializeObject(COMP)
                End With

            Case "Threats"

                With C
                    TH.Name = .Name
                    TH.Description = .Description
                    TH.RiskId = .RiskId
                    TH.RiskName = .RiskName
                    TH.LibraryId = .LibraryId
                    TH.Labels = .Labels
                    TH.Reference = .Reference
                    TH.Automated = .Automated
                    TH.StatusName = .StatusName
                    TH.IsHidden = C.ishidden
                    TH.CompanyId = .CompanyId
                    TH.IsDefault = .isDefault
                    TH.DateCreated = .DateCreated
                    TH.LastUpdated = .LastUpdated
                    TH.DepartmentId = .DepartmentId
                    TH.DepartmentName = .DepartmentName
                    TH.IsSystemDepartment = .IsSystemDepartment
                    TH.LibrayId = .LibraryId
                End With

                ' have to do this as original method will erase SRs from Threat

                With P
                    .EntityType = "Threats"
                    .Model = JsonConvert.SerializeObject(TH)
                End With



            Case "Attributes"
                Dim COMP As New tmAttribute
                COMP = C
                P.EntityType = "Threats"

                With P
                    .EntityType = "Attributes"
                    .Model = JsonConvert.SerializeObject(COMP)
                End With

        End Select

        jBody$ = JsonConvert.SerializeObject(P)
        jSon = getAPIData("/api/threatframework/master/addedit", True, jBody)
        If Val(jSon) Then hideItem = True

        P.ActionType = "TF_COMPONENT_UPDATED"

        'Console.WriteLine("PAYLOAD:" + vbCrLf + jBody + vbCrLf + "RESPONSE:" + vbCrLf + jSon)
        'now change the name to include "_TM"
        '{"LibraryId"10,"EntityType":"Components","Model":"{\"Id\":5761,\"Name\":\"Third Party Web Service_hidden\",\"Description\":\"<p>This component represents a standardized way of integrating Web-based applications using the XML, SOAP, WSDL And UDDI open standards over an Internet protocol backbone. </p><p>It Is located out of your network And managed/controlled by a 3rd party. These web applications operate in a client-server model.</p>\",\"ImagePath\":\"/ComponentImage/imageedit_5_6025455498202111181857525963.png\",\"Labels\":\"ThreatModeler,AppSec And InfraSec,Third Party Web Service\",\"Version\":\"\",\"LibraryId\":10,\"ComponentTypeId\":3,\"ComponentTypeName\":\"Component\",\"Color\":\"/ComponentImage/imageedit_5_6025455498202111181857525963.png\",\"Guid\":\"ac207d5c-d579-4dee-8a08-2e95227befff\",\"IsHidden\":false,\"DiagralElementId\":0,\"ResourceTypeValue\":null,\"Attributes\":null,\"LibrayId\":10}","ActionType":"TF_COMPONENT_UPDATED","IsCopy":false}

        If P.EntityType = "Components" Then
            If unHide = False Then C.Name += "_hidden" Else C.Name = Replace(C.Name, "_hidden", "")
            P.Model = JsonConvert.SerializeObject(C)
            jBody$ = JsonConvert.SerializeObject(P)
            jSon = getAPIData("/api/threatframework/componentmaster/addedit", True, jBody, True)

            'Console.WriteLine("PAYLOAD:" + vbCrLf + jBody + vbCrLf + "RESPONSE:" + vbCrLf + jSon)
        Else
            If unHide = False Then C.Name += "_hidden" Else C.Name = Replace(C.Name, "_hidden", "")
            Select Case P.EntityType
                Case "Threats"
                    Call addEditTH(C)
                Case "SecurityRequirements"
                    Call addEditSR(C)
            End Select
        End If

    End Function
    Public Function addEditCOMP(C As tmComponent, Optional actioNtypE$ = "TF_COMPONENT_ADDED", Optional overrideLIB As Integer = 0, Optional overrideCTYPE As Integer = 0, Optional overrideCTname$ = "") As Boolean
        addEditCOMP = False
        Dim P As New addCompClass

        Dim newC As New addCompModel
        With newC
            .ImagePath = C.ImagePath '"/ComponentImage/DefaultComponent.jpg"
            If overrideCTYPE Then .ComponentTypeId = overrideCTYPE.ToString Else .ComponentTypeId = C.ComponentTypeId.ToString
            .RiskId = 1
            .CodeTypeId = 1
            .DataClassificationId = 1
            .Name = C.Name
            If overrideCTYPE Then .ComponentTypeName = overrideCTname Else .ComponentTypeName = C.ComponentTypeName
            .Labels = C.Labels
            If overrideLIB Then .LibrayId = overrideLIB Else .LibrayId = C.LibraryId
            .Description = C.Description
        End With

        With P
            If overrideLIB Then .LibraryId = overrideLIB Else .LibraryId = C.LibraryId
            .EntityType = "Components"
            .Model = JsonConvert.SerializeObject(newC)
            .ActionType = actioNtypE
            .IsCopy = False
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/componentmaster/addedit", True, jBody, True)

        If InStr(json, "true") Then addEditCOMP = True


        '       "{""LibraryId"":94,""EntityType"":""Components"",""Model"":""{\""Id\"":3105,\""Name\"":\""Log Manager\"",\""Description\"":\""<p>A scalable solution for collecting, analyzing, storing and reporting on large volumes of network and security event logs. 
        'Log Manager collects, analyzes, stores And reports On Network security log events To help organizations protect themselves against threats, attacks And Security breaches. </p><p>Log Manager helps organizations meet compliance monitoring And reporting requirements And it can be 
        'seamlessly upgraded For a higher level Of threat protection .</p>\"",\ ""ImagePath\"":\""/ComponentImage/462642202006180335030840.png\"",\""Labels\"":null,\""Version\"":\""\"",\""LibraryId\"":94,\""ComponentTypeId\"":66,
        '\""ComponentTypeName\"":\""Security Control\"",\""Color\"":\""/ComponentImage/462642202006180335030840.png\"",\""Guid\"":\""1b6b1c50-0268-4109-a23d-63297ce72194\"",\""IsHidden\"":false,\""DiagralElementId\"":0,\""ResourceTypeValue\"":null,\""listThreats\"":[],\""listDirectSRs\
        '"":[],\""listTransSRs\"":[],\""listAttr\"":[],\""isBuilt\"":false,\""duplicateSRs\"":[],\""CompID\"":3105,\""CompName\"":\""Log Manager\"",\""TypeName\"":\""Security Control\"",\""NumTH\"":0,\""NumSR\"":0}""}"


        '{"LibraryId":10,"EntityType":"Components","Model":"{\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":\"3\",\"RiskId\":1,
        '\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"TestComp\",\"ComponentTypeName\":\"Component\",\"Labels\":\"ThreatModeler\",\"LibrayId\":10,\"Description\":\"Description here\"}","ActionType":"TF_COMPONENT_ADDED","IsCopy":false}



    End Function

    Public Function editCOMP(C As tmComponent, Optional actioNtypE$ = "TF_COMPONENT_UPDATED", Optional overrideLIB As Integer = 0, Optional overrideCTYPE As Integer = 0, Optional overrideCTname$ = "") As Boolean
        editCOMP = False
        Dim P As New addCompClass

        With C
            '            .ImagePath = C.ImagePath '"/ComponentImage/DefaultComponent.jpg"
            If overrideCTYPE Then .ComponentTypeId = overrideCTYPE.ToString Else .ComponentTypeId = C.ComponentTypeId.ToString
            '           .RiskId = 1
            '          .CodeTypeId = 1
            '         .DataClassificationId = 1
            .Name = C.Name
            If overrideCTYPE Then .ComponentTypeName = overrideCTname Else .ComponentTypeName = C.ComponentTypeName
            .Labels = C.Labels
            '        If overrideLIB Then .LibrayId = overrideLIB Else .LibrayId = C.LibraryId
            .Description = C.Description
        End With

        With P
            If overrideLIB Then .LibraryId = overrideLIB Else .LibraryId = C.LibraryId
            .EntityType = "Components"
            .Model = JsonConvert.SerializeObject(C)
            .ActionType = actioNtypE
            .IsCopy = False
        End With

        Dim jBody$ = JsonConvert.SerializeObject(P)
        Dim json$ = getAPIData("/api/threatframework/componentmaster/addedit", True, jBody, True)

        If InStr(json, "true") Then editCOMP = True

    End Function
    Public Function threatsOfEntity(C As Object, ntyType$) As List(Of tmProjThreat)
        threatsOfEntity = New List(Of tmProjThreat)
        Dim cResp As New tmCompQueryResp

        Dim jBody$ = ""
        Dim modeL$ = JsonConvert.SerializeObject(C) 'submits serialized model with escaped quotes

        Dim cReq As New tmTFQueryRequest
        With cReq
            .Model = modeL
            .LibraryId = C.LibraryId
            .EntityType = ntyType
        End With

        jBody = JsonConvert.SerializeObject(cReq)

        Dim json$ = getAPIData("/api/threatframework/query", True, jBody)

        json = Mid(json, 2, InStr(json, "],") - 1)
        json = Mid(json, InStr(json, "],") + 1)

        threatsOfEntity = JsonConvert.DeserializeObject(Of List(Of tmProjThreat))(json)


    End Function

    Public Function entityType(C As Object) As String
        entityType$ = ""
        Dim ntyType$ = ""

        Select Case Replace(C.GetType.ToString, "TMutil.", "")
            Case "tmComponent" : ntyType = "Components"
            Case "tmProjThreat" : ntyType = "Threats"
            Case "tmProjSecReq" : ntyType = "SecurityRequirements"
            Case "tmAttribute" : ntyType = "Attributes"
        End Select
        Return ntyType
    End Function

    Public Function bestMatch(C As Object, Optional ByVal showMatch As Boolean = True) As Object
        bestMatch = New tmComponent With {.Id = 0}

        Dim ndX As Integer = 0
        Dim namE$ = C.name

        Dim ntyType$ = entityType(C)

        'Console.WriteLine("Finding Best Match of " + ntyType)

        Select Case ntyType$
            Case "Threats"
                Dim destTH As New tmProjThreat
                Dim gString$ = ""
                If IsNothing(C.guid) = False Then
                    gString = C.guid.ToString
                End If
                destTH = guidTHREAT(gString)
                If destTH.Id = 0 Then
                    ndX = ndxTHbyName(C.name)
                    If ndX <> -1 Then destTH = lib_TH(ndX)
                    If showMatch Then Console.WriteLine("Best Match NAME '" + C.name + " [" + C.id.ToString + "/" + C.guid.ToString + "] at " + tmFQDN + " [" + destTH.Id.ToString + "/" + gString + "]")
                Else
                    If showMatch Then Console.WriteLine("Best Match GUID '" + C.name + " [" + C.id.ToString + "/" + C.guid.ToString + "] at " + tmFQDN + " [" + destTH.Id.ToString + "/" + destTH.Guid.ToString + "]")
                End If
                Return destTH
            Case "Components"
                Dim destC As New tmComponent
                destC = guidCOMP(C.guid.ToString)
                If destC.Id = 0 Then
                    ndX = ndxCompbyName(C.name)
                    If ndX <> -1 Then destC = lib_Comps(ndX)
                    If showMatch Then Console.WriteLine("Best Match NAME '" + C.name + " [" + C.id.ToString + "/" + C.guid.ToString + "] at " + tmFQDN + " [" + destC.Id.ToString + "/" + destC.Guid.ToString + "]")
                Else
                    If showMatch Then Console.WriteLine("Best Match GUID '" + C.name + " [" + C.id.ToString + "/" + C.guid.ToString + "] at " + tmFQDN + " [" + destC.Id.ToString + "/" + destC.Guid.ToString + "]")
                End If
                Return destC

            Case "Attributes"
                Dim destAT As New tmAttribute
                destAT = guidATTR(C.guid.ToString)
                If destAT.Id = 0 Then
                    ndX = ndxATTRbyName(C.name)
                    If ndX <> -1 Then destAT = lib_AT(ndX)
                    If showMatch Then Console.WriteLine("Best Match NAME '" + C.name + " [" + C.id.ToString + "/" + C.guid.ToString + "] at " + tmFQDN + " [" + destAT.Id.ToString + "/" + destAT.Guid.ToString + "]")
                Else
                    If showMatch Then Console.WriteLine("Best Match GUID '" + C.name + " [" + C.id.ToString + "/" + C.guid.ToString + "] at " + tmFQDN + " [" + destAT.Id.ToString + "/" + destAT.Guid.ToString + "]")
                End If
                Return destAT

            Case "SecurityRequirements"
                Dim destSR As New tmProjSecReq
                destSR = guidSR(C.guid.ToString)
                If destSR.Id = 0 Then
                    ndX = ndxSRbyName(C.name)
                    If ndX <> -1 Then destSR = lib_SR(ndX)
                    If showMatch Then Console.WriteLine("Best Match NAME '" + C.name + " [" + C.id.ToString + "/" + C.guid.ToString + "] at " + tmFQDN + " [" + destSR.Id.ToString + "/" + destSR.Guid.ToString + "]")
                Else
                    If showMatch Then Console.WriteLine("Best Match GUID '" + C.name + " [" + C.id.ToString + "/" + C.guid.ToString + "] at " + tmFQDN + " [" + destSR.Id.ToString + "/" + destSR.Guid.ToString + "]")

                End If
                Return destSR

        End Select
    End Function

    Public Function comp_ATTRmapping(idAT As Integer, Optional ByVal filen$ = "comp_template_mappings.txt") As String
        comp_ATTRmapping = ""
        Dim FF As Integer = FreeFile()
        FileOpen(FF, filen, OpenMode.Input)

        Dim attrS As New Collection 'the IDs
        Dim a$ = ""
        Do Until EOF(FF) = True

            a$ = LineInput(FF)
            attrS = CSVtoCOLL(Mid(a, InStr(a, "ATTR:") + 1))
            If grpNDX(attrS, idAT.ToString) Then
                comp_ATTRmapping += Replace(Mid(a, 1, InStr(a, " ATTR:") - 1), "COMP:", "") + ","
            End If
        Loop

        If Len(comp_ATTRmapping) Then comp_ATTRmapping = Mid(comp_ATTRmapping, 1, Len(comp_ATTRmapping) - 1)

        FileClose(FF)
    End Function

    Public Function attNumThreats(AT As tmAttribute, Optional ByVal optionName As String = "") As Integer
        attNumThreats = 0
        For Each O In AT.Options
            If Len(optionName) Then
                If LCase(optionName) <> LCase(O.Name) Then GoTo skipThis
            End If
            attNumThreats += O.Threats.Count
skipThis:
        Next

    End Function

    Public Function findDUPS(C As Object, TM As TM_Client, Optional ByVal findGUIDSonly As Boolean = False) As List(Of tmComponent)
        findDUPS = New List(Of tmComponent)

        Dim ndX As Integer = 0
        Dim namE$ = C.name
        Dim guiD$ = ""
        If IsNothing(C.Guid) = False Then
            guiD = C.guid.ToString
        End If

        Dim ntyType$ = entityType(C)

        'Console.WriteLine("Finding Best Match of " + ntyType)

        Select Case ntyType$
            Case "Threats"
                For Each tH In TM.lib_TH
                    If IsNothing(tH.Guid) = False Then
                        If LCase(tH.Guid.ToString) = LCase(guiD) Then
                            Dim newT As New tmComponent
                            With newT
                                .Id = tH.Id
                                .Name = tH.Name
                                .Guid = tH.Guid
                                .Description = tH.Description
                                findDUPS.Add(newT)
                            End With
                            GoTo nextOneT
                        End If
                    End If
                    If findGUIDSonly Then GoTo nextOneT
                    If LCase(tH.Name) = LCase(namE) Then
                        Dim newT As New tmComponent
                        With newT
                            .Id = tH.Id
                            .Name = tH.Name
                            .Guid = tH.Guid
                            findDUPS.Add(newT)
                        End With

                    End If
nextOneT:
                Next

            Case "Components"
                For Each tH In TM.lib_Comps
                    If IsNothing(tH.Guid) = False Then
                        If LCase(tH.Guid.ToString) = LCase(guiD) Then
                            Dim newT As New tmComponent
                            With newT
                                .Id = tH.Id
                                .Name = tH.Name
                                .Guid = tH.Guid
                                .Description = tH.Description
                                findDUPS.Add(newT)
                            End With
                            GoTo nextOneC
                        End If
                    End If
                    If findGUIDSonly Then GoTo nextOneC
                    If LCase(tH.Name) = LCase(namE) Then
                        Dim newT As New tmComponent
                        With newT
                            .Id = tH.Id
                            .Name = tH.Name
                            .Guid = tH.Guid
                            findDUPS.Add(newT)
                        End With

                    End If
nextOneC:
                Next

            Case "Attributes"
                For Each tH In TM.lib_AT
                    If IsNothing(tH.Guid) = False Then
                        If LCase(tH.Guid.ToString) = LCase(guiD) Then
                            Dim newT As New tmComponent
                            With newT
                                .Id = tH.Id
                                .Name = tH.Name
                                .Guid = tH.Guid
                                '.Description = tH.Description
                                findDUPS.Add(newT)
                            End With
                            GoTo nextOneA
                        End If
                    End If
                    If findGUIDSonly Then GoTo nextOneA
                    If LCase(tH.Name) = LCase(namE) Then
                        Dim newT As New tmComponent
                        With newT
                            .Id = tH.Id
                            .Name = tH.Name
                            .Guid = tH.Guid
                            findDUPS.Add(newT)
                        End With

                    End If
nextOneA:
                Next

            Case "SecurityRequirements"
                For Each tH In TM.lib_SR
                    If IsNothing(tH.Guid) = False Then
                        If LCase(tH.Guid.ToString) = LCase(guiD) Then
                            Dim newT As New tmComponent
                            With newT
                                .Id = tH.Id
                                .Name = tH.Name
                                .Guid = tH.Guid
                                .Description = tH.Description
                                findDUPS.Add(newT)
                            End With
                            GoTo nextOneS
                        End If
                    End If
                    If findGUIDSonly Then GoTo nextOneS
                    If LCase(tH.Name) = LCase(namE) Then
                        Dim newT As New tmComponent
                        With newT
                            .Id = tH.Id
                            .Name = tH.Name
                            .Guid = tH.Guid
                            findDUPS.Add(newT)
                        End With

                    End If
nextOneS:
                Next
        End Select
    End Function


    Public Function threatsByBackend(C As Object) As List(Of tmBackEndThreats)
        threatsByBackend = New List(Of tmBackendThreats)
        Dim cResp As New tmCompQueryResp

        Dim jBody$ = ""
        Dim modeL$ = JsonConvert.SerializeObject(C) 'submits serialized model with escaped quotes

        Dim cReq As New tmTFQueryRequest
        With cReq
            .Model = modeL
            .LibraryId = C.LibraryId
            .EntityType = "Widgets"
        End With

        jBody = JsonConvert.SerializeObject(cReq)

        Dim json$ = getAPIData("/api/threatframework/query", True, jBody)

        json = Mid(json, InStr(json, "[") + 1)
        json = Mid(json, 1, InStr(json, "]") - 1) + "]"

        threatsByBackend = JsonConvert.DeserializeObject(Of List(Of tmBackendThreats))(json)


    End Function

    Public Sub buildCompObj(ByVal C As tmComponent, Optional ByVal quickLookup As Boolean = False, Optional ByVal quieT As Boolean = False, Optional ByVal noSRs As Boolean = False) ' As tmComponent 'C As tmComponent, ByRef TH As List(Of tmProjThreat), ByRef SR As List(Of tmProjSecReq)) As tmComponent
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

            For Each T In .listThreats
                ndX = ndxTHlib(T.Id) ', lib_TH)
                C.listThreats.Add(lib_TH(ndX))
            Next

            If quickLookup Then GoTo skipForProtocolsAndControls

            For Each T In .listAttr
                Dim ndxAT = ndxATTR(T.Id)
                If ndxAT <> -1 Then C.listAttr.Add(lib_AT(ndxAT))
            Next

            For Each T In .listDirectSRs
                ndX = ndxSRlib(T.Id) ', lib_SR)
                If ndX <> -1 Then C.listDirectSRs.Add(lib_SR(ndX))
            Next

        End With


        If C.ComponentTypeName = "Protocols" Or C.ComponentTypeName = "Security Control" Or noSRs = True Then GoTo skipForProtocolsAndControls

        Dim possibleTransSRs As New List(Of tmProjSecReq)

        possibleTransSRs = addTransitiveSRs(C)


        For Each TSR In possibleTransSRs
            '    'C.listTransSRs.Add(TSR)
            If C.numLabels > 0 Then
                If numMatchingLabels(C.Labels, TSR.Labels) / C.numLabels > 0.9 Then
                    C.listTransSRs.Add(TSR)
                End If
                '        'Console.WriteLine(TSR.Id.ToString + TSR.Name)
            End If
        Next


        'GoTo skipForProtocolsAndControls

quickLKP1:
        'Console.WriteLine(vbCrLf)

        For Each AT In C.listAttr
            For Each O In AT.Options
                For Each TH In O.Threats
                    Dim possibleAttrSRs As New List(Of tmProjSecReq)
                    If quickLookup = False Then possibleAttrSRs = addTransitiveSRsOFattr(C, O)

                    Dim ndxT As Integer = ndxTHlib(TH.Id)

                    If ndxT <> -1 Then
                        For Each TSR In lib_TH(ndxT).listLinkedSRs
                            'C.listTransSRs.Add(TSR)
                            Dim ndxS As Integer = ndxSRlib(TSR)

                            'dont do label matching here but rather keep all SRs linked to threat and match labels when wanted
                            'If C.numLabels > 0 Then
                            'If numMatchingLabels(C.Labels, lib_SR(ndxS).Labels) / C.numLabels > 0.9 Then
                            TH.listLinkedSRs.Add(TSR)
                            'End If
                        Next
                    End If

                    '                    If possibleAttrSRs.Count Then Console.WriteLine("Label matching " + possibleAttrSRs.Count.ToString + " down to " + TH.listLinkedSRs.Count.ToString + " SRs")
                Next
            Next
        Next


skipForProtocolsAndControls:

        C.isBuilt = True

    End Sub

    Public Function returnSRsWithLabelMatch(T As tmProjThreat, C As tmComponent) As Collection
        returnSRsWithLabelMatch = New Collection
        Dim ndxS As Integer = 0

        With returnSRsWithLabelMatch
            For Each TSRID In T.listLinkedSRs
                Dim TSR As New tmProjSecReq
                ndxS = ndxSRlib(TSRID)
                If ndxS = -1 Then GoTo nextSR
                TSR = lib_SR(ndxS)

                If C.numLabels > 0 Then
                    If numMatchingLabels(C.Labels, TSR.Labels) / C.numLabels > 0.9 Then
                        .Add(TSR.Id)
                    End If
                    '        'Console.WriteLine(TSR.Id.ToString + TSR.Name)
                End If
nextSR:
            Next

        End With

    End Function

    Private Function addTransitiveSRs(ByRef C As tmComponent) As List(Of tmProjSecReq)
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

        'Console.WriteLine(vbCrLf)

        For K = 0 To C.listThreats.Count - 1
            T = C.listThreats(K)
            'here we get the transitive SRs
            ndxTH = ndxTHlib(T.Id) ', lib_TH)

            'removed check for ISBUILT to save time - need to refine this

            lib_TH(ndxTH).listLinkedSRs = New Collection
            ' End If

            cReq = New tmTFQueryRequest
            modeL$ = JsonConvert.SerializeObject(T) 'submits serialized model with escaped quotes

            Console.SetCursorPosition(0, Console.CursorTop - 1)
            Console.WriteLine(spaces(80))
            Console.SetCursorPosition(0, Console.CursorTop - 1)

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


            For L = 0 To transSRs.Count - 1
                    'add all transitive SRs to lib_TH Threat
                    Dim TSR As New tmProjSecReq
                    TSR = transSRs(L)
                If grpNDX(lib_TH(ndxTH).listLinkedSRs, TSR.Id) = 0 Then lib_TH(ndxTH).listLinkedSRs.Add(TSR.Id) 'add to Threat linkedSRs collection if unique 

                Dim ndXLIB As Integer = ndxSRlib(TSR.Id) 'here ndx represents index of SR in SR Library
                    If numMatchingLabels(C.Labels, lib_SR(ndXLIB).Labels) / C.numLabels > 0.9 Then

                    ndX = ndxSR(TSR.Id, addTransitiveSRs)
                    If ndX = -1 Then
                        addTransitiveSRs.Add(lib_SR(ndXLIB))
                    Else
                        C.duplicateSRs.Add(TSR.Id)
                    End If

duplicateHere:
                End If
                Next
skipTheLoad:
            lib_TH(ndxTH).isBuilt = True

        Next

        GC.Collect()

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

            If ndxTH = -1 Then
                Console.WriteLine("WARNING - DB ERROR: Threat has been assigned that no longer exists: " + O.Threats(K).Name + " [" + O.Threats(K).Id.ToString + "]")
                GoTo skipTheLoad
            End If

            If lib_TH(ndxTH).isBuilt = True Then
                ' added doevents as this seems to trip// suspect lowlevel probs with isBuilt prop
                'this seems to happen a lot
                'if we already know SRs, just add them from coll and skip web request
                For Each TSRID In lib_TH(ndxTH).listLinkedSRs
                    If grpNDX(C.duplicateSRs, TSRID) Then C.duplicateSRs.Add(TSRID)

                    If ndxSR(TSRID, addTransitiveSRsOFattr) <> -1 Then
                        C.duplicateSRs.Add(TSRID)
                        GoTo duphere
                    End If
                    ndX = ndxSRlib(TSRID) ', lib_SR)
                    If ndX <> -1 Then addTransitiveSRsOFattr.Add(lib_SR(ndX))
duphere:
                Next
                GoTo skipTheLoad
                        ' Else
                    End If

                    lib_TH(ndxTH).listLinkedSRs = New Collection
                    ' End If

                    cReq = New tmTFQueryRequest
                    modeL$ = JsonConvert.SerializeObject(T) 'submits serialized model with escaped quotes

            Console.SetCursorPosition(0, Console.CursorTop - 1)
            Console.WriteLine(spaces(80))
            Console.SetCursorPosition(0, Console.CursorTop - 1)

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

                    If grpNDX(C.duplicateSRs, TSR.Id) Then C.duplicateSRs.Add(TSR.Id)
                    If ndxSR(TSR.Id, addTransitiveSRsOFattr) <> -1 Then
                        C.duplicateSRs.Add(TSR.Id)
                        GoTo duplicate
                    End If
                    'ndX = ndxSRlib(TSR.Id) ', lib_SR)
                    addTransitiveSRsOFattr.Add(TSR)
duplicate:
                        Next
                    End With
skipTheLoad:
            If ndxTH <> -1 Then lib_TH(ndxTH).isBuilt = True

        Next



    End Function

    Public Sub defineTransSRs(ByVal threatID As Long, Optional ByVal louDloG As Boolean = False)
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

        'Console.WriteLine("Getting SRs of threat")

        cReq = New tmTFQueryRequest
        modeL$ = JsonConvert.SerializeObject(T) 'submits serialized model with escaped quotes

        With cReq
            .Model = modeL
            .LibraryId = T.LibraryId
            .EntityType = "Threats"
        End With


        If louDloG Then
            Console.WriteLine("Pulling ThreatID " + T.Id.ToString + "        ")
            Console.SetCursorPosition(0, Console.CursorTop - 1)
        End If

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
            If grpNDX(T.listLinkedSRs, TSR.Id) <> 0 Then GoTo duplicate
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

    Public Function removeLabelFromSR(ByRef SR As tmProjSecReq, label$) As Boolean
        removeLabelFromSR = False

        Dim PL As New Collection
        PL = CSVtoCOLL(SR.Labels)

        Dim K As Integer = 0
        For K = PL.Count To 1 Step -1
            If LCase(PL(K)) = LCase(label) Then
                PL.Remove(K)
                removeLabelFromSR = True
            End If
        Next

        Dim lbL$ = ""

        If removeLabelFromSR Then
            For K = 1 To PL.Count
                lbL += PL(K) + ","
            Next
            lbL = Mid(lbL, 1, Len(lbL) - 1)
            SR.Labels = lbL
        End If

    End Function

    Public Function matchLabelsOnSR(ByRef SR As tmProjSecReq, labels$) As Boolean
        matchLabelsOnSR = False

        Dim Clabels As New Collection
        Dim SRlabels As New Collection
        Clabels = CSVtoCOLL(labels)
        SRlabels = CSVtoCOLL(SR.Labels)

        For Each C In Clabels
            If grpNDX(SRlabels, C) = 0 Then
                SRlabels.Add(C)
            End If
        Next
        matchLabelsOnSR = True

        Dim lbL$ = ""

        Dim K As Integer = 0

        For K = 1 To SRlabels.Count
            lbL += SRlabels(K) + ","
        Next
        lbL = Mid(lbL, 1, Len(lbL) - 1)
        SR.Labels = lbL

    End Function

    Public Class kisCreateModel_setProject
        '{"Id":0,
        '"Name""test_meth_inst2",
        '"RiskId":1,"Labels":"",
        '"Version":"1","IsValidFile":false,
        '"Type":"AWSCloudApplication",
        '"CreatedThrough":"Blank","UserPermissions":[],
        '"GroupPermissions":[]}

        Public Id As Integer
        Public Name As String
        Public RiskId As Integer
        Public Labels As String
        Public Version As Integer
        Public isValidFile As Boolean
        Public [Type] As String
        Public CreatedThrough As String
        Public UserPermissions As List(Of String)
        Public GroupPermissions As List(Of String)
    End Class

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
                    Dim newT As New tmSimpleThreatSR
                    newT.Name = T.ThreatName
                    newT.StatusName = T.StatusName
                    newT.Id = T.ThreatId
                    'newT.Description = T.Description
                    ' notes is missing!
                    'newT.RiskName = T.ActualRiskName
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

    Public Function ndxMiscByName(name$, libSearch As List(Of tmMiscTrigger)) As Integer
        ndxMiscByName = -1
        name = LCase(name)
        Dim ndX As Integer = 0
        For Each P In libSearch
            If LCase(P.Name) = name Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function

    Public Function ndxATTRbyName(namE$) As Integer
        ndxATTRbyName = -1
        namE = LCase(namE)
        Dim ndX As Integer = 0
        For Each P In lib_AT
            If LCase(P.Name) = namE Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function

    Public Function ndxATTRofList(ID As Long, L As List(Of tmAttribute)) As Integer
        ndxATTRofList = -1

        Dim ndX As Integer = 0
        For Each P In L
            If P.Id = ID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function


    Public Function ndxTHofList(ID As Long, L As List(Of tmProjThreat)) As Integer
        ndxTHofList = -1

        Dim ndX As Integer = 0
        For Each P In L
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

    Public Function ndxSRlibByName(namE$) As Integer
        ndxSRlibByName = -1

        Dim ndX As Integer = 0
        For Each P In lib_SR
            If P.Name = namE Then
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

    Public Function ndxLibByName(namE$, libS As List(Of tmLibrary)) As Integer
        ndxLibByName = -1

        Dim ndX As Integer = 0
        For Each P In libS
            If LCase(P.Name) = LCase(namE) Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next
    End Function

    Public Function ndxCompType(ID As Long) As Integer
        ndxCompType = -1

        Dim ndX As Integer = 0
        For Each P In componentTypes
            If P.Id = ID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function

    Public Function ndxCompTypeByName(namE$) As Integer
        ndxCompTypeByName = -1

        Dim ndX As Integer = 0
        For Each P In componentTypes
            If LCase(P.Name) = LCase(namE) Then
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
    Public Function ndxCompbyGUID(GUID$) As Integer ', ByRef alL As List(Of tmComponent)) As Integer
        ndxCompbyGUID = -1

        'had to change this as passing byref from multi-threaded main causing issues

        Dim ndX As Integer = 0
        For Each P In lib_Comps
            If P.Guid.ToString = GUID Then
                Return ndX
                Exit Function
            End If
            ndX += 1
        Next

    End Function

    Public Function guidTHREAT(GUID$) As tmProjThreat ', ByRef alL As List(Of tmComponent)) As Integer
        'ndxCompbyGUID = -1
        guidTHREAT = New tmProjThreat
        'had to change this as passing byref from multi-threaded main causing issues

        For Each P In lib_TH
            If P.Guid.ToString = GUID Then
                Return P
                Exit Function
            End If
        Next

    End Function
    Public Function guidCOMP(GUID$) As tmComponent ', ByRef alL As List(Of tmComponent)) As Integer
        'ndxCompbyGUID = -1
        guidCOMP = New tmComponent
        'had to change this as passing byref from multi-threaded main causing issues

        For Each P In lib_Comps
            If P.Guid.ToString = GUID Then
                Return P
                Exit Function
            End If
        Next

    End Function
    Public Function guidSR(GUID$) As tmProjSecReq ', ByRef alL As List(Of tmComponent)) As Integer
        'ndxCompbyGUID = -1
        guidSR = New tmProjSecReq
        'had to change this as passing byref from multi-threaded main causing issues

        For Each P In lib_SR
            If P.Guid.ToString = GUID Then
                Return P
                Exit Function
            End If
        Next

    End Function

    Public Function guidATTR(GUID$) As tmAttribute ', ByRef alL As List(Of tmComponent)) As Integer
        'ndxCompbyGUID = -1
        guidATTR = New tmAttribute
        'had to change this as passing byref from multi-threaded main causing issues

        For Each P In lib_AT
            If P.Guid.ToString = GUID Then
                Return P
                Exit Function
            End If
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

    Public Function ndxTHbyName(name$) As Integer ', ByRef alL As List(Of tmComponent)) As Integer
        ndxTHbyName = -1

        'had to change this as passing byref from multi-threaded main causing issues

        Dim ndX As Integer = 0
            For Each P In lib_TH
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

        nodeS = JsonConvert.DeserializeObject(Of List(Of tmNodeData))(nodeJSON) ', SS) ', SS)

        getProject = JsonConvert.DeserializeObject(Of tmModel)(jsoN)
        getProject.Nodes = nodeS

        '        Call addThreats(getProject, projID) 'add in details of threats not inside Diagram API (eg Protocols)
    End Function

End Class

Public Class entityUpdate
    '{"propertyIds":[101],"propertyEntityType":"Threats","sourceEntityIds":[449],"LibraryId":1,"EntityType":"Components","IsAddition":false}
    Public propertyIds As List(Of Integer)
    Public propertyEntityType$
    Public sourceEntityIds As List(Of Integer)
    Public LibraryId As Integer
    Public EntityType$
    Public IsAddition As Boolean

    Public Sub New()
        propertyIds = New List(Of Integer)
        sourceEntityIds = New List(Of Integer)
    End Sub
End Class

Public Class entityUpdateBID
    '{"propertyIds":[101],"propertyEntityType":"Threats","sourceEntityIds":[449],"LibraryId":1,"EntityType":"Components","IsAddition":false}
    Public propertyIds As List(Of Integer)
    Public propertyEntityType$
    Public sourceEntityIds As List(Of Integer)
    Public LibraryId As Integer
    Public EntityType$
    Public IsAddition As Boolean
    Public BackendId As Integer

    Public Sub New()
        propertyIds = New List(Of Integer)
        sourceEntityIds = New List(Of Integer)
    End Sub
End Class


Public Class updateEntityObject
    Public LibraryId As Integer
    Public EntityType$
    Public Model$ ' As tmProjSecReq

End Class

Public Class addCompClass
    '{"LibraryId":10,"EntityType":"Components","Model":"{\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":\"3\",\"RiskId\":1,\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"TestComp\",\"ComponentTypeName\":\"Component\",\"Labels\":\"ThreatModeler\",\"LibrayId\":10,\"Description\":\"Description here\"}","ActionType":"TF_COMPONENT_ADDED","IsCopy":false}
    Public LibraryId As Integer
    Public EntityType$
    Public Model$
    Public ActionType$
    Public IsCopy As Boolean

End Class

Public Class editEntityTH
    Public LibraryId As Integer
    Public EntityType$
    Public ActionType$
    Public Model$
End Class
Public Class addSRClass
    '"{\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":85,\"RiskId\":1,\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"NewSR\",\"Labels\":\"ThreatModeler\",\"LibrayId\":0,\"Description\":\"SR Desc\",\"IsCopy\":false}"}
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

'{"LibraryId"0,"EntityType":"Threats","Model":"{\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":85,\"RiskId\":1,\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"New Threat\",\"Labels\":\"ThreatModeler\",\"LibrayId\":0,\"Description\":\"Descript\",\"IsCopy\":false}"}
Public Class addTHClass
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

Public Class editTHClass
    '{"LibraryId":0,"EntityType":"Threats","ActionType":"TF_ENTITY_HIDDEN",
    '"Model":"{\"Id\":24,\"Guid\":\"1d68189d-90aa-4cbd-8899-8a980d95c312\",
    '\"Name\":\"Attack through Shared Data\",\"Description\":\"<p>An attacker exploits a data structure shared between multiple applications Or an application pool to affect application behavior. Data may be shared between multiple applications Or between multiple threads of a single application. </p><p>Data sharing Is usually accomplished through mutual access to a single memory location. If an attacker can manipulate this shared data (usually by co-opting one of the applications Or threads) the other applications Or threads using the shared data will often continue to trust the validity of the compromised shared data And use it in their calculations. </p><p>This can result in invalid trust assumptions, corruption of additional data through the normal operations of the other users of the shared data, Or even cause a crash Or compromise of the sharing applications.\\n</p><p>Reference <a href =\\\ "https://capec.mitre.org/data/definitions/124.html\\\" > https : //capec.mitre.org/data/definitions/124.html</a> </p>\",
    '\"RiskId\":3,\"RiskName\":\"Medium\",\"LibraryId\":1,
    '\"Labels\":\"CAPEC-124,CAPEC-116,CAPEC-117,CAPEC-153,CAPEC-167,CAPEC-168,CAPEC-194,CAPEC-200,CAPEC-204,CAPEC-215,CAPEC-257,CAPEC-261,CAPEC-277,CAPEC-37,CAPEC-39,CAPEC-255,CAPEC-536,CAPEC-545,CAPEC-567,CAPEC-569,CAPEC-610,OWASP-A6,OWASP-A5-Security-Misconfiguration\",
    '\"Reference\":null,\"Automated\":false,\"ThreatAttributes\":null,\"StatusName\":null,\"IsHidden\":true,\"CompanyId\":0,
    '\"SharingType\":null,
    '\"Readonly\":false,\"IsDefault\":false,\"DateCreated\":\"0001-01-01T0000:00\",\"LastUpdated\":\"0001-01-01T00:00:00\",
    '\"DepartmentId\":0,
    '\"DepartmentName\":null,\"Version\":null,\"IsSystemDepartment\":false,\"LibrayId\":0}"}
    Public Name$
    Public Description$
    Public RiskId As Integer
    Public RiskName$
    Public LibraryId As Integer
    Public Labels$
    Public Reference$
    Public Automated As Boolean
    Public ThreatAttributes$
    Public StatusName$
    Public IsHidden As Boolean
    Public CompanyId As Integer
    Public SharingType As Integer
    Public [Readonly] As Boolean
    Public IsDefault As Boolean
    Public DateCreated$
    Public LastUpdated$
    Public DepartmentId As Integer
    Public DepartmentName$
    Public Version$
    Public IsSystemDepartment As Boolean
    Public LibrayId As Integer
End Class


Public Class addCompModel
    Public ImagePath$
    Public ComponentTypeId$ ' As Integer
    Public RiskId As Integer
    Public CodeTypeId As Integer
    Public DataClassificationId As Integer
    Public Name$
    Public ComponentTypeName$
    Public Labels$
    Public LibrayId As Integer
    Public Description$
End Class
Public Class tfRequest
    'obj used to request Threats, SRs, Components
    'unpublished API
    '    "EntityType""SecurityRequirements","LibraryId":"0","ShowHidden":false
    Public EntityType$
    Public LibraryId As Integer '0 = ALL
    Public ShowHidden As Boolean
End Class

Public Class threatStatusUpdate
    Public Id As Long
    Public ThreatId As Long
    Public ProjectId As Long
    Public StatusId As Integer
End Class
Public Class complianceDetails
    Public Id As Integer
    Public Name As String
    Public Sections As List(Of compSection)

    Public Sub New()
        Sections = New List(Of compSection)
    End Sub
End Class

Public Class compSection
    Public Id As Integer
    Public Name As String
    Public Title As String
    Public Domain As String
    Public SecurityRequirements As List(Of compSRs)
    Public Sub New()
        SecurityRequirements = New List(Of compSRs)
    End Sub
End Class

Public Class compSRs
    Public Id As Integer
    Public Name As String
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
Public Class tmTemplate
    Public Id As Long
    Public Name$
    Public LibraryId As Integer
    Public [Type] As String
    Public Json$
    '    "Description": null,
    '    "Labels": "",
    '    "Image": null,
    '    "DiagramData": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADoAAAA4CAYAAACsc+sjAAAJGElEQVRoQ+2ae4xcVR3HP+fce+ex291tF1qKtCW1YiGihArCIIhpi0ALgjFFIGp5KJAIakUi/NF0pyVFCALiE1AohsRaAyrBiqHUojA0gBLRNIDaQOkSCi3L7nZ2Zuc+jvndOdvOTnfq7AzONmlvdnPncc75/b6/7+91zh3FQXKpgwQnh4A2wvRV97zgdZQG56S6D5uWaGtr9wuFeeAcY0w4S2tnhlIcjnY8QBFGRWOiHVEUblNav4HW//KcxD+H8gP54cLwtjuvOfV1pZRpRI+x5rwvjC7pWZeY1d19c/ec404zEXOiIJhmVKRNGAFWV1O+G3tXyooeuQNau2KDSHvOG2Bef/e1l9d27nzmnp6eHlmoqaspoNetXj/1qBM/Pq+YH3g8OamLUn6AMAxQEhEVAMajYdkQBsdLkEx3MNT/joD/7Fvv5R+/9+qT/PGsVTm2KaDL1255IZGeNC/0h5UxYvSmltsXgzEo7eAkU1Fp9+Czqy457vSWA73hvtyNk488+pZSYff7D7AKjRixrfMw+nq3Lr31ylN+0QjYhim4ac2Lz6UPm3JyWCw1Ind8c4zB7Wij1Ne/ctWlx68Y3+Ty6IaBXrvst5unLTjxlGg4wIRhEyv9D7UN6KSLdh3eeeql7N23nd/TUqDXr9yQM4HJTPrITLzJ7YT5YYxvATeYiPYAMOXs7KQSOG0ehe27yL/yFjqts3esOKvFQLNP5JTjZUp9u0kc0UV65lTcjhSEBsKwXFQkg8ZKx2/GJkJJjjblLB2/VuBq0Aq/b5Ditl34/QUSYkx/eOKAxuoHIZEANFEM1u1sw+1Koz0P5TlozylHSfmvXE9H7pHBBCHGDwiLPn5/nrC/SJgvoFwH5TjgyESNCUoTCNQ2AGU2IBKlSyEmDMARRRU64YIorTU6Zs4QimGiMvvhcIgKI0wUoVwXlXTQWu9pLmKbyPuJALosuyGnHTdj/XL/+cG68GguK/KgricnakwUZO9YsaDFMbpqU05rnYmisJEkOM45Bq09YTx7+/IzWwv0gouX5U7IfDUz2NeLdqRH/f9dSmm6Js/ixc33ZR95aHVrgV54ybdz7Z3TM7OPPQe/MEAY+RhhN/bCelxxf4YpZ2tJRI6TiNl85e8PE4SF7KNr72ot0CWX9+SiwM/4fp4jjjqR7qlzSSQn4Q8X8IN8GbTtScpltRZ4M6r8aO2QSEzC8VIUh/rYtWMLO99+hWSqA4XOrluzovVAMSojihWGdqJw6Oj6AJ2TZ9DWOR3PTcd10ZgQIyUkbvor8NrSKQaQxl0pJx7rlwoM9veyu7+XwYFetHJJtU1GcoGC7Lo1PRMBlMxe7Q1RFMRMRuLGBrxEGtdrx3WTuF4qBqQFkJQhE8SlxQ+KBH6RoDREEBRsCfLK9/L+dE/VPQCAVsTbSHcTqxcRhWXwYRSgjHxS3m9qdNwEOMKm41aAkk5q7H32hAC96IpVOROFltHxZNzKWK3/pER6X+242XX3L2+t637mgqtzU6fPyZSK+YZPE+o3jyHd1sXOt7Zm1z/yw9YC/dTCi3Od3dMz3YfPolgYJJL2rdldSxXyeAcjWTjVxo43X2Uo/1726Q3rWgt0wXlX5krFfCaV7mDK4TNxHY8gCuKtSq0Yq4fB2FZKg1G4rofvF9n19mv4wTDJRDq74bH7Wwt0/uLLcxgyUhLkQEwKe3vHFFLpTjwvGZ9qCmBp1CXDSonZp9dVoKW8OC7S/YhHhKEfgyvk+yns7ovLiqwlWRhjshvXr5kIoCZORqKgZNOgVIx3JHKCl0ikcOKy4uF6aaTexrtNYQtTrq2Sl00Qz/ODEmFpGN8vEIQBWulySVJ7j0iBiQW6r0uK++49z5XX9lR370bUdksjXdNIfJcNUfNqPdAFiy5/3mBOqifu4u6nRge439OHqsWVMiuf/P2DrT0cm7/oMhHYULzUZ5yxRqkrNq5/4IFG5je1zZi/6LKXgbmNCG5gTm7j+jWfbGDeqBa7oflnnP3FIz3HXQg0dKhcr1BjzNKBmclf/vXeeyfmkcSIoqcvvnRKwiS/h+JYjJkDTKsXxNjjzHZQW5UyT4b52as3beoJmluv+R3yKPnnnntdssTuOaEx0xxNyii1EMNchZlhMN2gujDKQRkJGTni7wfeQantwBZlzNNhRBETbN/0xw9theafoo0o2FSMNmvlVs4/BLSV1m6FrEOMtsLKrZQxkYxeC7wHPNQKwNVAJwN9FYJfAh4FbgMG61DoHKAX+EcdY6XPfx74RB1jmx5SDXQK8C7wNvAM8DkrYSOwoEqaPCKrfB4hP6vZBawEbq8aK1uS6hOvSqB7j/pGT6yWUVkWxzpwqrXOPqfKI0B/B1wIdANPAPOATwNPAV8BrgROtYzcDPwB+JU1jDAvjcCZwFHAd4DFgDQFPwZusdqKoq/az+cDvwG+DMiPIsaSIZ4lMsWQZwHibT8DfgCcDPzI3mUd+exPlTarxegIUBl7g3XdbwF3Wmalud4C3AokgGPsTkZA/Rp4DJA1JgHXA88CXwLOt2P/XfGIVNb8sDXGxdZg4j3VMmYD4llnANcBwrYYSoz8HHAkIDrKd7LRECPvaR3rAXoN8BNgOSDsCbvC0Ecta9LXzgI+CGyyhhlx3aOB84DTgOOBjwGXAGstUAmRI+zc/wD3W2+pJeNr1kNk3l3A3bav3grI/2bgWKujeNSfa7WA1a4rhtgAiGuJwsKMxKFYUlxwqXXpsYBKzIrwGcA3LZivVwEVN+8EhC0Z+6BlpZaMN4ElwE3WaJKxfwo8bcNIDDhyiauL58RXLUYFyMPAF6yCf7Fg5URBwP4N+C7wc6DDMtoGyP5UYlq2bTLnNZutrwK+YWOsklHRQTK6uL4kPvlO5tSS8XlAmB+yBAiDZ9s4L1gj7QDkh1ejDgXqKS8Sc3fYxSV7PgJcYAFIMlhmgb5h4+sia8QTgEUVyUcMc2MFowPWtSSOZD3J8hISwnItGd+vqARSmiT+xaACTIwusS6XEDXqQKDRhqHdAh8rxQvD+YpykrJJYX97SvEGYanyqiWjC5ANePV4mSvJT3QS+aOuRoFWr3PAvz8E9ICnaJwKHmJ0nAY74IcfNIz+Fzd/jmZVvtBTAAAAAElFTkSuQmCC",
    '    "LibraryName": "Mike Horty",
    '    "isUpdate": false
    Public Description$
    Public Labels$
    Public Image$
    Public DiagramData$
    Public LibraryName$
    Public isUpdate As Boolean
    Public ReadOnly Property TempId
        Get
            Return Id
        End Get
    End Property
    Public ReadOnly Property TemplateName
        Get
            Return Name
        End Get
    End Property

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
    Public Labels As String
    Public CreateDate As DateTime
    Public RiskId As Integer
    Public RiskName$
    Public CreatedByName$
    Public LastModifiedByName$
    Public LastModifiedDate As DateTime
    'Public RiskName As Integer
    Public [Type] As String
    Public CreatedByUserEmail$
    Public Model As tmModel
    'Public nodesFromThreats As Integer 'beginning at 300k, nodes from threats like HTTPS
End Class

Public Class tmDept
    Public Id As Integer
    Public Name$
    Public TotalLicenses As Integer
    Public UsedLicenses As Integer
    Public LibraryId As Integer
End Class
Public Class tmUser
    ' {
    '"Id" 271,
    '"Name": "Ahsan",
    '"Email": "ahsan.rehman@threatmodeler.com",
    '"Username": "ahsan.rehman@threatmodeler.com",
    '"UserRoleId": 1,
    '"UserRoleName": "Admin",
    '"Activated": false,
    '"LastLogin": "2022-02-02T20:25:25.263Z",
    '"DepartmentId": 1,
    '"UserDepartmentName": "Corporate",
    '"AspNetUserId": "81285fde-a096-4220-8b3d-7ba9b1e4838f",
    '"GroupUsers": null,
    '"TransferOwnershipToUserId": null,
    '"TransferOwnership": false,
    '"ReceiveLicenseEmailNotification": true,
    '"LicenseRenewalEmailNotificationSent": false,
    '"Company": null,
    '"ApiKey": null

    Public Id As Integer
    Public Name As String
    Public Email As String
    Public Username As String
    Public UserRoleId As Integer
    Public UserRoleName As String
    Public Activated As Boolean
    Public LastLogin? As DateTime
    Public DepartmentId As Integer
    Public ApiKey As String

End Class

Public Class tmProjThreat
    ' these threats come from the DIAGRAM API as part of nodeArray
    Public Id As Long
    Public Guid? As System.Guid 'nulls
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

Public Class tmBackendThreats
    '{
    '             "Id": 341,
    '             "BackendId": 1,
    '             "ThreatId": 0,
    '             "ThreatName": "Cross Site Request Forgery",
    '             "BackendName": "None",
    '             "WidgetId": 0,
    '             "WidgetName": null,
    '             "LibraryId": 1
    '         },

    Public Id As Long
    Public BackendId As Integer
    Public ThreatId As Integer
    Public ThreatName$
    Public BackendName$
    Public WidgetId As Integer


End Class

Public Class tmMiscTrigger
    '"Id": 1,
    '"Name": "Executive",
    '"Labels": "",
    '"LibraryId": 1,
    '"Guid": "c27efce9-788f-4534-9e95-285bd37f8c93",
    '"Description": null,
    '"IsHidden": false
    Public Id As Long
    Public Name$
    Public Labels$
    Public Description$
    Public LibraryId As Integer
    Public Guid? As System.Guid
    Public isHidden As Boolean

End Class
Public Class tmCompQueryResp
    Public listThreats As List(Of tmProjThreat)
    Public listDirectSRs As List(Of tmProjSecReq)
    'Public listTransSRs As List(Of tmProjSecReq)
    Public listAttr As List(Of tmAttribute)
End Class

Public Class tmAttrCreate
    '{"LibraryId":0,"EntityType":"Attributes","Model":"{\"ImagePath\":\"/ComponentImage/DefaultComponent.jpg\",\"ComponentTypeId\":3,\"RiskId\":1,\"CodeTypeId\":1,\"DataClassificationId\":1,\"Name\":\"New ATTR\",\"Labels\":\"ThreatModeler\",\"LibrayId\":0,\"Description\":\"Some ATTR Desc\",\"IsCopy\":false}"}
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

Public Class tmAttribute
    Public Id As Long
    Public Guid? As Guid
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
    Public Guid? As System.Guid
    Public IsHidden As Boolean
    Public DiagralElementId As Integer
    Public ResourceTypeValue$
    'Public Attributes$

    Public listThreats As List(Of tmProjThreat)
    Public listDirectSRs As List(Of tmProjSecReq)
    Public listTransSRs As List(Of tmProjSecReq)
    Public listAttr As List(Of tmAttribute)
    Public isBuilt As Boolean

    Public modelsPresent As List(Of Integer)
    Public numInstancesPerModel As List(Of Integer)

    Public duplicateSRs As Collection

    Public Function numLabels() As Integer
        If Len(Labels) = 0 Then
            Return 0
        Else
            Return numCHR(Labels, ",") + 1
        End If
    End Function

    Public Function getModelNDX(ID As Integer) As Integer
        getModelNDX = -1

        Dim ctR As Integer = 0

        For Each M In modelsPresent
            If M = ID Then
                Return ctR
                Exit Function
            End If
            ctR += 1
        Next

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
        duplicateSRs = New Collection
        numInstancesPerModel = New List(Of Integer)
        modelsPresent = New List(Of Integer)
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
    Public Guid? As System.Guid
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
    Public listThreats As List(Of tmSimpleThreatSR)
    Public listSecurityRequirements As List(Of tmSimpleThreatSR)
    Public ComponentTypeName$
    Public ComponentName$
    Public Notes$
    Public Id As Long
    Public threatCount As Integer

    Public Sub New()
        Me.listSecurityRequirements = New List(Of tmSimpleThreatSR)
        Me.listThreats = New List(Of tmSimpleThreatSR)

    End Sub
End Class

Public Class tmSimpleThreatSR
    Public Id As Integer
    Public Name$
    Public StatusName$
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

