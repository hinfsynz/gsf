' 09-26-06
' PCP: 10/11/2007 - Added GeneratePassword() shared function and enforced strong password rule in EncryptPassword()

Imports System.Data
Imports System.Data.SqlClient
Imports System.Security.Principal
Imports System.Text
Imports System.Text.RegularExpressions
Imports TVA.Common
Imports TVA.Identity
Imports TVA.Data.Common
Imports TVA.Math.Common

Namespace Application

    <Serializable()> _
    Public Class User

#Region " Member Declaration "

        Private m_username As String
        Private m_password As String
        Private m_firstName As String
        Private m_lastName As String
        Private m_companyName As String
        Private m_phoneNumber As String
        Private m_emailAddress As String
        Private m_securityQuestion As String
        Private m_securityAnswer As String
        Private m_isExternal As Boolean
        Private m_isLockedOut As Boolean
        Private m_passwordChangeDateTime As System.DateTime
        Private m_accountCreatedDateTime As System.DateTime
        Private m_isAuthenticated As Boolean
        Private m_exists As Boolean
        Private m_groups As List(Of Group)
        Private m_roles As List(Of Role)
        Private m_applications As List(Of Application)

        Private Const MinimumPasswordLength As Integer = 8
        Private Const StrongPasswordRegex As String = "^.*(?=.{8,})(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).*$"

#End Region

#Region " Code Scope: Public "

        Public Sub New(ByVal username As String, ByVal dbConnection As SqlConnection)

            MyClass.New(username, Nothing, dbConnection)

        End Sub

        Public Sub New(ByVal username As String, ByVal password As String, ByVal dbConnection As SqlConnection)

            MyClass.New(username, password, dbConnection, Nothing)

        End Sub

        Public Sub New(ByVal username As String, ByVal password As String, ByVal dbConnection As SqlConnection, ByVal applicationName As String)

            If dbConnection IsNot Nothing Then
                If dbConnection.State <> System.Data.ConnectionState.Open Then dbConnection.Open()
                ' We'll retrieve all the data we need in a single trip to the database by calling the stored 
                ' procedure 'RetrieveApiData' that will return 3 tables to us:
                ' Table1 (Index 0): Information about the user.
                ' Table2 (Index 1): Groups the user is a member of.
                ' Table3 (Index 2): Roles that are assigned to the user either directly or through a group.
                Dim userData As DataSet = RetrieveDataSet("RetrieveApiData", dbConnection, username, applicationName)

                If userData.Tables(0).Rows.Count > 0 Then
                    ' User does exist in the security database.
                    m_exists = True
                    With userData.Tables(0)
                        m_username = .Rows(0)("UserName").ToString()
                        m_password = .Rows(0)("UserPassword").ToString()
                        m_firstName = .Rows(0)("UserFirstName").ToString()
                        m_lastName = .Rows(0)("UserLastName").ToString()
                        m_companyName = .Rows(0)("UserCompanyName").ToString()
                        m_phoneNumber = .Rows(0)("UserPhoneNumber").ToString()
                        m_emailAddress = .Rows(0)("UserEmailAddress").ToString()
                        m_securityQuestion = .Rows(0)("UserSecurityQuestion").ToString()
                        m_securityAnswer = .Rows(0)("UserSecurityAnswer").ToString()
                        If .Rows(0)("UserIsExternal") IsNot DBNull.Value Then
                            m_isExternal = Convert.ToBoolean(.Rows(0)("UserIsExternal"))
                        End If
                        If .Rows(0)("UserIsLockedOut") IsNot DBNull.Value Then
                            m_isLockedOut = Convert.ToBoolean(.Rows(0)("UserIsLockedOut"))
                        End If
                        If .Rows(0)("UserPasswordChangeDateTime") IsNot DBNull.Value Then
                            m_passwordChangeDateTime = Convert.ToDateTime(.Rows(0)("UserPasswordChangeDateTime"))
                        End If
                        m_accountCreatedDateTime = Convert.ToDateTime(.Rows(0)("UserAccountCreatedDateTime"))
                    End With

                    m_groups = New List(Of Group)
                    With userData.Tables(1)
                        For i As Integer = 0 To .Rows.Count - 1
                            m_groups.Add(New Group(.Rows(i)("GroupName").ToString(), .Rows(i)("GroupDescription").ToString()))
                        Next
                    End With

                    m_roles = New List(Of Role)
                    m_applications = New List(Of Application)
                    With userData.Tables(2)
                        For i As Integer = 0 To .Rows.Count - 1
                            Dim application As New Application(.Rows(i)("ApplicationName").ToString(), .Rows(i)("ApplicationDescription").ToString())
                            m_roles.Add(New Role(.Rows(i)("RoleName").ToString(), .Rows(i)("RoleDescription").ToString(), application))

                            ' Since an application can have multiple roles, we're going to add an application to the list
                            ' of application only if it doesn't exist already.
                            If Not m_applications.Contains(application) Then m_applications.Add(application)
                        Next
                    End With

                    userData.Dispose()

                    If m_isExternal Then
                        ' User is external according to the security database.
                        If password IsNot Nothing Then
                            ' We'll validate the password against the security database.
                            m_isAuthenticated = (EncryptPassword(password) = m_password)
                        End If
                    Else
                        ' User is internal according to the security database.
                        If password IsNot Nothing Then
                            ' We'll validate the password against the active directory.
                            m_isAuthenticated = New UserInfo(m_username, "TVA", True).Authenticate(password)
                        Else
                            ' When an internal user is found in the security database, he/she is considered
                            ' autheticated and we are not required to validate the password unless one is 
                            ' provided to us by the caller.
                            m_isAuthenticated = True
                        End If
                    End If
                End If
            End If

        End Sub

        Public ReadOnly Property Username() As String
            Get
                Return m_username
            End Get
        End Property

        Public ReadOnly Property Password() As String
            Get
                Return m_password
            End Get
        End Property

        Public ReadOnly Property FirstName() As String
            Get
                Return m_firstName
            End Get
        End Property

        Public ReadOnly Property LastName() As String
            Get
                Return m_lastName
            End Get
        End Property

        Public ReadOnly Property CompanyName() As String
            Get
                Return m_companyName
            End Get
        End Property

        Public ReadOnly Property PhoneNumber() As String
            Get
                Return m_phoneNumber
            End Get
        End Property

        Public ReadOnly Property EmailAddress() As String
            Get
                Return m_emailAddress
            End Get
        End Property

        Public ReadOnly Property SecurityQuestion() As String
            Get
                Return m_securityQuestion
            End Get
        End Property

        Public ReadOnly Property SecurityAnswer() As String
            Get
                Return m_securityAnswer
            End Get
        End Property

        Public ReadOnly Property IsExternal() As Boolean
            Get
                Return m_isExternal
            End Get
        End Property

        Public ReadOnly Property IsLockedOut() As Boolean
            Get
                Return m_isLockedOut
            End Get
        End Property

        ''' <summary>
        ''' Gets the date and time when user must change the password.
        ''' </summary>
        ''' <value></value>
        ''' <returns>The date and time when user must change the password.</returns>
        Public ReadOnly Property PasswordChangeDateTime() As System.DateTime
            Get
                Return m_passwordChangeDateTime
            End Get
        End Property

        ''' <summary>
        ''' Gets the date and time when user account was created.
        ''' </summary>
        ''' <value></value>
        ''' <returns>The date and time when user account was created.</returns>
        Public ReadOnly Property AccountCreatedDateTime() As System.DateTime
            Get
                Return m_accountCreatedDateTime
            End Get
        End Property

        Public ReadOnly Property IsAuthenticated() As Boolean
            Get
                Return m_isAuthenticated
            End Get
        End Property

        Public ReadOnly Property Exists() As Boolean
            Get
                Return m_exists
            End Get
        End Property

        Public ReadOnly Property Groups() As List(Of Group)
            Get
                Return m_groups
            End Get
        End Property

        Public ReadOnly Property Roles() As List(Of Role)
            Get
                Return m_roles
            End Get
        End Property

        Public ReadOnly Property Roles(ByVal applicationName As String) As List(Of Role)
            Get
                '**** Added By Mehul
                Dim applicationRoles As New List(Of Role)()

                If m_roles IsNot Nothing Then
                    For i As Integer = 0 To m_roles.Count - 1
                        If String.Compare(m_roles(i).Application.Name, applicationName, True) = 0 Then
                            applicationRoles.Add(m_roles(i))
                        End If
                    Next
                End If

                Return applicationRoles
            End Get
        End Property

        Public ReadOnly Property Applications() As List(Of Application)
            Get
                Return m_applications
            End Get
        End Property

        Public Function FindGroup(ByVal groupName As String) As Group

            If m_groups IsNot Nothing Then
                For i As Integer = 0 To m_groups.Count - 1
                    If String.Compare(m_groups(i).Name, groupName, True) = 0 Then
                        ' User does belong to the specified group.
                        Return m_groups(i)
                    End If
                Next
            End If
            Return Nothing

        End Function

        Public Function FindRole(ByVal roleName As String) As Role

            If m_roles IsNot Nothing Then
                For i As Integer = 0 To m_roles.Count - 1
                    If String.Compare(m_roles(i).Name, roleName, True) = 0 Then
                        ' User is in the specified role.
                        Return m_roles(i)
                    End If
                Next
            End If
            Return Nothing

        End Function

        Public Function FindRole(ByVal roleName As String, ByVal applicationName As String) As Role

            Dim role As Role = FindRole(roleName)
            If role IsNot Nothing AndAlso String.Compare(role.Application.Name, applicationName, True) = 0 Then
                ' User is in the specified role and the specified role belongs to the specified application.
                Return role
            End If
            Return Nothing

        End Function

        Public Function FindApplication(ByVal applicationName As String) As Application

            If m_applications IsNot Nothing Then
                For i As Integer = 0 To m_applications.Count - 1
                    If String.Compare(m_applications(i).Name, applicationName, True) = 0 Then
                        ' User has access to the specified application.
                        Return m_applications(i)
                    End If
                Next
            End If
            Return Nothing

        End Function

        ''' <summary>
        ''' Returns users roles for an application.
        ''' </summary>
        ''' <param name="applicationName">Application Name</param>
        ''' <returns>List of roles for specified application</returns>
        ''' <remarks></remarks>
        <Obsolete("Use the Roles property that takes an application name as a parameter instead. This function will be removed in a future release.", True)> _
        Public Function FindApplicationRoles(ByVal applicationName As String) As List(Of Role)

            '**** Added By Mehul
            Dim applicationRoles As New List(Of Role)()

            If m_roles IsNot Nothing Then
                For i As Integer = 0 To m_roles.Count - 1
                    If String.Compare(m_roles(i).Application.Name, applicationName, True) = 0 Then
                        applicationRoles.Add(m_roles(i))
                    End If
                Next
            End If

            Return applicationRoles

        End Function

#Region " Shared "

        Public Shared Function GeneratePassword(ByVal length As Integer) As String

            If length >= MinimumPasswordLength Then
                Dim password As Char() = CreateArray(Of Char)(length)

                ' ASCII character ranges:
                ' Digits - 48 to 57
                ' Upper case - 65 to 90
                ' Lower case - 97 to 122

                ' Out of the minimum of 8 characters in the password, we'll make sure that the password contains
                ' at least 2 digits and 2 upper case letter, so that the password meets the strong password rule.
                Dim digits As Integer = 0
                Dim upperCase As Integer = 0
                Dim minSpecialChars As Integer = 2
                For i As Integer = 0 To password.Length - 1
                    If digits < minSpecialChars Then
                        password(i) = Chr(CInt(RandomBetween(48, 57)))
                        digits += 1
                    ElseIf upperCase < minSpecialChars Then
                        password(i) = Chr(CInt(RandomBetween(65, 90)))
                        upperCase += 1
                    Else
                        password(i) = Chr(CInt(RandomBetween(97, 122)))
                    End If
                Next

                ' We have a random password that meets the strong password rule, now we'll shuffle it to make it 
                ' even more random.
                Dim temp As Char
                Dim swapIndex As Integer
                For i As Integer = 0 To password.Length - 1
                    swapIndex = CInt(RandomBetween(0, password.Length - 1))
                    temp = password(swapIndex)
                    password(swapIndex) = password(i)
                    password(i) = temp
                Next

                Return New String(password)
            Else
                Throw New ArgumentException(String.Format("Password length should be at least {0} characters.", MinimumPasswordLength))
            End If

        End Function

        Public Shared Function EncryptPassword(ByVal password As String) As String

            If Regex.IsMatch(password, StrongPasswordRegex) Then
                ' We prepend salt text to the password and then has it to make it even more secure.
                Return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile("O3990\P78f9E66b:a35_V�6M13�6~2&[" & password, "SHA1")
            Else
                ' Password does not meet the strong password rule defined below, so we don't encrypt the password.
                With New StringBuilder()
                    .Append("Password does not meet the following criteria:")
                    .AppendLine()
                    .Append("- Password must be at least 8 characters")
                    .AppendLine()
                    .Append("- Password must contain at least 1 digit")
                    .AppendLine()
                    .Append("- Password must contain at least 1 upper case letter")
                    .AppendLine()
                    .Append("- Password must contain at least 1 lower case letter")

                    Throw New InvalidOperationException(.ToString())
                End With
            End If

        End Function

#End Region

#End Region

    End Class

End Namespace