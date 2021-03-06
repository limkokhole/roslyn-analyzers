﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.NetCore.Analyzers.Security.UnitTests
{
    public class ReviewCodeForSqlInjectionVulnerabilitiesTests : TaintedDataAnalyzerTestBase
    {
        protected override DiagnosticDescriptor Rule => ReviewCodeForSqlInjectionVulnerabilities.Rule;

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ReviewCodeForSqlInjectionVulnerabilities();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new ReviewCodeForSqlInjectionVulnerabilities();
        }

        [Fact]
        public void HttpRequest_Form_LocalString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string input = Request.Form[""in""];
            if (Request.Form != null && !String.IsNullOrWhiteSpace(input))
            {
                SqlCommand sqlCommand = new SqlCommand()
                {
                    CommandText = input,
                    CommandType = CommandType.Text,
                };
            }
        }
     }
}
            ",
                GetCSharpResultAt(20, 21, 15, 28, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_LocalString_VB_Diagnostic()
        {
            VerifyBasicWithDependencies(@"
Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Web
Imports System.Web.UI

Namespace VulnerableWebApp
    Partial Public Class WebForm
        Inherits System.Web.UI.Page

        Protected Sub Page_Load(sender As Object, e As EventArgs)
            Dim input As String = Me.Request.Form(""In"")
            If Me.Request.Form IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(input) Then
                Dim sqlCommand As SqlCommand = New SqlCommand() With {.CommandText = input,
                                                                      .CommandType = CommandType.Text}
            End If
        End Sub
    End Class
End Namespace
            ",
                GetBasicResultAt(16, 72, 14, 35, "Property SqlCommand.CommandText As String", "Sub WebForm.Page_Load(sender As Object, e As EventArgs)", "Property HttpRequest.Form As NameValueCollection", "Sub WebForm.Page_Load(sender As Object, e As EventArgs)"));
        }

        [Fact]
        public void HttpRequest_Form_DelegateInvocation_OutParam_LocalString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public delegate void StringOutputDelegate(string input, out string output);

        public static StringOutputDelegate StringOutput;

        protected void Page_Load(object sender, EventArgs e)
        {
            StringOutput(Request.Form[""in""], out string input);
            if (Request.Form != null && !String.IsNullOrWhiteSpace(input))
            {
                SqlCommand sqlCommand = new SqlCommand()
                {
                    CommandText = input,
                    CommandType = CommandType.Text,
                };
            }
        }
     }
}
            ",
                GetCSharpResultAt(24, 21, 19, 26, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_InterfaceInvocation_OutParam_LocalString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public interface IBlah { void StringOutput(string input, out string output); }

        public static IBlah Blah;

        protected void Page_Load(object sender, EventArgs e)
        {
            Blah.StringOutput(Request.Form[""in""], out string input);
            if (Request.Form != null && !String.IsNullOrWhiteSpace(input))
            {
                SqlCommand sqlCommand = new SqlCommand()
                {
                    CommandText = input,
                    CommandType = CommandType.Text,
                };
            }
        }
     }
}
            ",
                GetCSharpResultAt(24, 21, 19, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_LocalStringMoreBlocks_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string input;
            if (Request.Form != null)
            {
                input = Request.Form[""in""];
            }
            else
            {
                input = ""SELECT 1"";
            }

            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(27, 17, 18, 25, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_And_QueryString_LocalStringMoreBlocks_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string input;
            if (Request.Form != null)
            {
                input = Request.Form[""in""];
            }
            else
            {
                input = Request.QueryString[""in""];
            }

            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(27, 17, 18, 25, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"),
                GetCSharpResultAt(27, 17, 22, 25, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.QueryString", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_Direct_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = Request.Form[""in""],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(17, 17, 17, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_Substring_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = Request.Form[""in""].Substring(1),
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(17, 17, 17, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }


        [Fact]
        public void Sanitized_HttpRequest_Form_Direct_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE id < "" + int.Parse(Request.Form[""in""]).ToString(),
                CommandType = CommandType.Text,
            };
        }
     }
}
            ");
        }

        [Fact]
        public void Sanitized_HttpRequest_Form_TryParse_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Int16.TryParse(Request.Form[""in""], out short i))
            {
                SqlCommand sqlCommand = new SqlCommand()
                {
                    CommandText = ""SELECT * FROM users WHERE id < "" + i.ToString(),
                    CommandType = CommandType.Text,
                };
            }
        }
     }
}
            ");
        }


        [Fact]
        public void HttpRequest_Form_Item_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = Request[""in""],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(17, 17, 17, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_Item_Enters_SqlParameters_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = @username"",
                CommandType = CommandType.Text,
            };

            sqlCommand.Parameters.Add(""@username"", SqlDbType.NVarChar, 16).Value = Request[""in""];

            sqlCommand.ExecuteReader();
        }
     }
}
            ");
        }

        [Fact]
        public void HttpRequest_Form_Item_Sql_Constructor_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand(Request[""in""]);
        }
     }
}
            ",
                GetCSharpResultAt(15, 37, 15, 52, "SqlCommand.SqlCommand(string cmdText)", "void WebForm.Page_Load(object sender, EventArgs e)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }


        [Fact]
        public void HttpRequest_Form_Method_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string input = Request.Form.Get(""in"");
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(18, 17, 15, 28, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_LocalNameValueCollectionString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            System.Collections.Specialized.NameValueCollection nvc = Request.Form;
            string input = nvc[""in""];
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(19, 17, 15, 70, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_List_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            List<string> allTheInputs = new List<string>(new string[] { Request.Form[""in""] });
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = allTheInputs[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(20, 17, 17, 73, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact(Skip = "Would be nice to distinguish between tainted and non-tainted elements in the List, but for now we taint the entire List from its construction.  FxCop also has a false positive.")]
        public void HttpRequest_Form_List_SafeElement_Diagnostic()
        {
            // Would be nice to distinguish between tainted and non-tainted elements in the List, but for now we taint the entire List from its construction.  FxCop also has a false positive.

            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            List<string> list = new List<string>(new string[] { Request.Form[""in""] });
            list.Add(""SELECT * FROM users WHERE userid = 1"");
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = list[1],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ");
        }

        [Fact]
        public void HttpRequest_Form_Array_List_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string[] array = new string[] { Request.Form[""in""] };
            List<string> allTheInputs = new List<string>(array);
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = allTheInputs[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(21, 17, 17, 45, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }


        [Fact]
        public void HttpRequest_Form_Array_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string[] allTheInputs = new string[] { Request.Form[""in""] };
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = allTheInputs[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(20, 17, 17, 52, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_LocalStructNameValueCollectionString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public struct MyStruct
        {
            public NameValueCollection nvc;
            public string s;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            MyStruct myStruct = new MyStruct();
            myStruct.nvc = this.Request.Form;
            myStruct.s = myStruct.nvc[""in""];
            string input = myStruct.s;
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(28, 17, 23, 28, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_LocalStructConstructorNameValueCollectionString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"

namespace VulnerableWebApp
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public struct MyStruct
        {
            public MyStruct(NameValueCollection v)
            {
                this.nvc = v;
                this.s = null;
            }

            public NameValueCollection nvc;
            public string s;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            MyStruct myStruct = new MyStruct();
            myStruct.nvc = this.Request.Form;
            myStruct.s = myStruct.nvc[""in""];
            string input = myStruct.s;
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(35, 17, 30, 28, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_Form_LocalStructConstructorNameValueCollectionString_VB_Diagnostic()
        {
            VerifyBasicWithDependencies(@"
Imports System
Imports System.Collections.Specialized
Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Web
Imports System.Web.UI


Namespace VulnerableWebApp
    Public Structure MyStruct
        Public Sub MyStruct(v As NameValueCollection)
            Me.nvc = v
            Me.s = Nothing
        End Sub


        Public nvc As NameValueCollection
        Public s As String
    End Structure

    Partial Public Class WebForm
        Inherits System.Web.UI.Page

        Protected Sub Page_Load(sender As Object, e As EventArgs)
            Dim myStruct As MyStruct = New MyStruct()
            myStruct.nvc = Me.Request.Form
            myStruct.s = myStruct.nvc(""in"")
            Dim input As String = myStruct.s
            Dim sqlCommand As SqlCommand = New SqlCommand() With {.CommandText = input,
                                                                  .CommandType = CommandType.Text}
            End Sub
    End Class
End Namespace
",
                GetBasicResultAt(31, 68, 28, 28, "Property SqlCommand.CommandText As String", "Sub WebForm.Page_Load(sender As Object, e As EventArgs)", "Property HttpRequest.Form As NameValueCollection", "Sub WebForm.Page_Load(sender As Object, e As EventArgs)"));
        }

        [Fact]
        public void HttpRequest_UserLanguages_Direct_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = Request.UserLanguages[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(17, 17, 17, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "string[] HttpRequest.UserLanguages", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_UserLanguages_LocalStringArray_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string[] languages = Request.UserLanguages;
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = languages[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(18, 17, 15, 34, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "string[] HttpRequest.UserLanguages", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void HttpRequest_UserLanguages_LocalStringModified_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string language = ""SELECT * FROM languages WHERE language = '"" + Request.UserLanguages[0] + ""'"";
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = language,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(18, 17, 15, 78, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "string[] HttpRequest.UserLanguages", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void OkayInputLocalStructNameValueCollectionString_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public struct MyStruct
        {
            public NameValueCollection nvc;
            public string s;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            MyStruct myStruct = new MyStruct();
            myStruct.nvc = this.Request.Form;
            myStruct.s = myStruct.nvc[""in""];
            string input = myStruct.s;
            myStruct.s = ""SELECT 1"";
            input = myStruct.s;
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ");
        }

        [Fact]
        public void OkayInputConst_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = 'foo'"",
                CommandType = CommandType.Text,
            };
        }
     }
}
            ");
        }

        [Fact]
        public void DataBoundLiteralControl_DirectImplementation_Text()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Web.UI;

    public class SomeClass
    {
        public DataBoundLiteralControl Control { get; set; }

        public void Execute()
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = '"" + this.Control.Text + ""'"",
                CommandType = CommandType.Text,
            };
        }
    }
}
            ",
                GetCSharpResultAt(17, 17, 17, 74, "string SqlCommand.CommandText", "void SomeClass.Execute()", "string DataBoundLiteralControl.Text", "void SomeClass.Execute()"));
        }

        [Fact]
        public void DataBoundLiteralControl_Interface_Text()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Web.UI;

    public class SomeClass
    {
        public DataBoundLiteralControl Control { get; set; }

        public void Execute()
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = '"" + ((ITextControl) this.Control).Text + ""'"",
                CommandType = CommandType.Text,
            };
        }
    }
}
            ",
                GetCSharpResultAt(17, 17, 17, 74, "string SqlCommand.CommandText", "void SomeClass.Execute()", "string ITextControl.Text", "void SomeClass.Execute()"));
        }

        [Fact]
        public void HtmlInputButton_Value()
        {
            // HtmlInputButton derives from HtmlInputControl, and HtmlInputControl.Value is a tainted data source.
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Web.UI.HtmlControls;

    public class SomeClass
    {
        public HtmlInputButton Button { get; set; }

        public void Execute()
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = '"" + this.Button.Value + ""'"",
                CommandType = CommandType.Text,
            };
        }
    }
}
            ",
                GetCSharpResultAt(17, 17, 17, 74, "string SqlCommand.CommandText", "void SomeClass.Execute()", "string HtmlInputControl.Value", "void SomeClass.Execute()"));
        }

        [Fact]
        public void SimpleInterprocedural()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            MyDatabaseLayer layer = new MyDatabaseLayer();
            layer.MakeSqlInjection(taintedInput);
        }
    }

    public class MyDatabaseLayer
    {
        public void MakeSqlInjection(string sqlInjection)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"",
                CommandType = CommandType.Text,
            };
        }
    }
}",
                GetCSharpResultAt(27, 17, 15, 35, "string SqlCommand.CommandText", "void MyDatabaseLayer.MakeSqlInjection(string sqlInjection)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void SimpleLocalFunction()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            SqlCommand injectSql(string sqlInjection)
            {
                return new SqlCommand()
                {
                    CommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"",
                    CommandType = CommandType.Text,
                };
            };

            injectSql(taintedInput);
        }
    }
}",
                GetCSharpResultAt(21, 21, 15, 35, "string SqlCommand.CommandText", "SqlCommand injectSql(string sqlInjection)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void IntermediateMethodReturnsTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            string sqlCommandText = StillTainted(taintedInput);

            ExecuteSql(sqlCommandText);
        }

        protected string StillTainted(string sqlInjection)
        {
            return ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 15, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void IntermediateMethodReturnsTaintedButOutputUntainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            
            string sqlCommandText = StillTainted(taintedInput, out string notTaintedSqlCommandText);

            ExecuteSql(notTaintedSqlCommandText);
        }

        protected string StillTainted(string sqlInjection, out string notSqlInjection)
        {
            notSqlInjection = ""SELECT * FROM users WHERE userid = "" + Int32.Parse(sqlInjection);
            return ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void IntermediateMethodReturnsTaintedButRefUntainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            
            string notTaintedSqlCommandText = taintedInput;
            string sqlCommandText = StillTainted(taintedInput, ref notTaintedSqlCommandText);

            ExecuteSql(notTaintedSqlCommandText);
        }

        protected string StillTainted(string sqlInjection, ref string notSqlInjection)
        {
            notSqlInjection = ""SELECT * FROM users WHERE userid = "" + Int32.Parse(sqlInjection);
            return ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void IntermediateMethodReturnsUntaintedButOutputTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            
            string sqlCommandText = StillTainted(taintedInput, out string taintedSqlCommandText);

            ExecuteSql(taintedSqlCommandText);
        }

        protected string StillTainted(string input, out string sqlInjection)
        {
            sqlInjection = ""SELECT * FROM users WHERE username = '"" + input + ""'"";
            return ""SELECT * FROM users WHERE userid = "" + Int32.Parse(input);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
                GetCSharpResultAt(32, 17, 15, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void IntermediateMethodReturnsUntaintedButRefTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            
            string taintedSqlCommandText = null;
            string sqlCommandText = StillTainted(taintedInput, ref taintedSqlCommandText);

            ExecuteSql(taintedSqlCommandText);
        }

        protected string StillTainted(string input, ref string taintedSqlCommandText)
        {
            taintedSqlCommandText = ""SELECT * FROM users WHERE username = '"" + input + ""'"";
            return ""SELECT * FROM users WHERE userid = "" + Int32.Parse(input);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
                GetCSharpResultAt(33, 17, 15, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void IntermediateMethodReturnsNotTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            string sqlCommandText = NotTainted(taintedInput);

            ExecuteSql(sqlCommandText);
        }

        protected string NotTainted(string sqlInjection)
        {
            return ""SELECT * FROM users WHERE username = 'bob'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void IntermediateMethodSanitizesTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""userid""];

            string sqlCommandText = SanitizeTainted(taintedInput);

            ExecuteSql(sqlCommandText);
        }

        protected string SanitizeTainted(string sqlInjection)
        {
            return ""SELECT * FROM users WHERE userid = '"" + Int32.Parse(sqlInjection) + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void IntermediateMethodOutParameterTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 15, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void IntermediateMethodOutParameterNotTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            NotTainted(taintedInput, out string sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void NotTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = 'bob'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void IntermediateMethodOutParameterSanitizesTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""userid""];

            SanitizeTainted(taintedInput, out string sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void SanitizeTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE userid = '"" + Int32.Parse(sqlInjection) + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinaryReturnsDefaultStillTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            sqlCommandText = OtherDllStaticMethods.ReturnsDefault(sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(35, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinaryReturnsInputStillTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            sqlCommandText = OtherDllStaticMethods.ReturnsInput(sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(34, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinarySetsOutputToDefaultStillTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            OtherDllStaticMethods.SetsOutputToDefault(sqlCommandText, out string sqlToExecute);

            ExecuteSql(sqlToExecute);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(35, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinarySetsReferenceToDefaultStillTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlToExecute = null;
            OtherDllStaticMethods.SetsReferenceToDefault(sqlCommandText, ref sqlToExecute);

            ExecuteSql(sqlToExecute);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(36, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_TaintedObject_Property_ConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ConstructedInput + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(29, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_TaintedObject_Property_Default()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.Default + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_TaintedObject_Method_ReturnsConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsConstructedInput() + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(29, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_TaintedObject_Method_SetsOutputToConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            otherDllObj.SetsOutputToConstructedInput(out string outputParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_TaintedObject_Method_SetsReferenceToConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            string referenceParameter = ""not tainted"";
            otherDllObj.SetsReferenceToConstructedInput(ref referenceParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + referenceParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(32, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_TaintedObject_Method_ReturnsDefault()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault() + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_TaintedObject_Method_SetsReferenceToDefault()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string referenceParameter = ""not tainted"";
            otherDllObj.SetsReferenceToDefault(ref referenceParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + referenceParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(33, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_TaintedObject_Method_ReturnsDefault_UntaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault(""not tainted"") + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Property_ConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ConstructedInput + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Property_Default()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.Default + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_ReturnsConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsConstructedInput() + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_SetsOutputToConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            otherDllObj.SetsOutputToConstructedInput(out string outputParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_SetsReferenceToConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string referenceParameter = ""also not tainted"";
            otherDllObj.SetsReferenceToConstructedInput(ref referenceParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + referenceParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_ReturnsDefault()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault() + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_SetsReferenceToDefault()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainteed"");

            string referenceParameter = ""also not tainted"";
            otherDllObj.SetsReferenceToDefault(ref referenceParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + referenceParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_ReturnsDefault_UntaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault(""also not tainted"") + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_ReturnsDefault_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault(taintedInput) + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_ReturnsInput_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsInput(taintedInput) + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(29, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_ReturnsRandom_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsRandom(taintedInput) + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_SetsOutputToDefault_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            otherDllObj.SetsOutputToDefault(taintedInput, out string outputParameter);
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_SetsOutputToInput_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            otherDllObj.SetsOutputToInput(taintedInput, out string outputParameter);
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void CrossBinary_UntaintedObject_Method_SetsOutputToRandom_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            otherDllObj.SetsOutputToRandom(taintedInput, out string outputParameter);
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        public void NonMonotonicMergeAssert()
        {
            VerifyCSharpWithDependencies(@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class SettingData1
{
    public string Name { get; set; }
    public string CartType { get; set; }
}

public class SettingData2
{
    public string CartType { get; set; }
    public string Index { get; set; }
}

public class Settings
{
    public string DefaultIndex { get; set; }
    public List<SettingData1> Datas1 { get; set; }
    public List<SettingData2> Datas2 { get; set; }
}

public class Class1
{
    public Settings MySettings { get; set; }
    public string SiteCartType { get; set; }
    private SettingData1 GetDefaultData1(string contentType, string taintedInput)
    {
        var settings = MySettings;
        var defaultData = settings.Datas2.FirstOrDefault(x => x.CartType == taintedInput);
        var defaultIndex = defaultData != null ? defaultData.Index : ""0"";

        if (String.IsNullOrWhiteSpace(defaultIndex))
            defaultIndex = ""0"";

        if (!settings.Datas2.Any(x => String.Equals(x.CartType, taintedInput, StringComparison.OrdinalIgnoreCase)))
        {
            var patternIndex = String.IsNullOrWhiteSpace(settings.DefaultIndex) ? ""0"" : settings.DefaultIndex;
            if (String.Equals(taintedInput, SiteCartType, StringComparison.OrdinalIgnoreCase))
            {
                settings.Datas2.Add(new SettingData2 { Index = patternIndex, CartType = taintedInput });
                return settings.Datas1.Where(x => x.CartType == null).ElementAt(Convert.ToInt32(defaultIndex));
            }
            else
            {
                settings.Datas2.Add(new SettingData2 { Index = ""0"", CartType = taintedInput });
                return new SettingData1 { Name = ""Name"", CartType = taintedInput };
            }
        }

        var cartTypeSearch = settings.Datas1.Any(x => String.Equals(x.CartType, taintedInput, StringComparison.OrdinalIgnoreCase)) ? taintedInput : null;

        if (settings.Datas1.Any())
        {
            if (settings.Datas1.Where(x => x.CartType == cartTypeSearch).ElementAt(Convert.ToInt32(defaultIndex)) != null)
            {
                return settings.Datas1.Where(x => x.CartType == cartTypeSearch).ElementAt(Convert.ToInt32(defaultIndex));
            };
        }

        return new SettingData1 { Name = ""Name"", CartType = taintedInput };
    }

    public void ProcessRequest()
    {
        string tainted = HttpContext.Current.Request.Form[""taintedinput""];
        GetDefaultData1(HttpContext.Current.Request.ContentType, tainted);
    }
}
");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn-analyzers/issues/1891")]
        public void PointsToAnalysisAssertsLocationSetsComparison()
        {
            VerifyCSharp(@"
using System;
using System.IO;
using System.Threading;
using System.Web;

public interface IContext
{
    HttpContext HttpContext { get; }
}

public class CaptureStream : Stream
{
    public CaptureStream(Stream innerStream)
    {
        _innerStream = innerStream;
        _captureStream = new MemoryStream();
    }

    private readonly Stream _innerStream;
    private readonly MemoryStream _captureStream;

    public override bool CanRead
    {
        get { return _innerStream.CanRead; }
    }

    public override bool CanSeek
    {
        get { return _innerStream.CanSeek; }
    }

    public override bool CanWrite
    {
        get { return _innerStream.CanWrite; }
    }

    public override long Length
    {
        get { return _innerStream.Length; }
    }

    public override long Position
    {
        get { return _innerStream.Position; }
        set { _innerStream.Position = value; }
    }

    public override long Seek(long offset, SeekOrigin direction)
    {
        return _innerStream.Seek(offset, direction);
    }

    public override void SetLength(long length)
    {
        _innerStream.SetLength(length);
    }

    public override void Close()
    {
        _innerStream.Close();
    }

    public override void Flush()
    {
        if (_captureStream.Length > 0)
        {
            OnCaptured();
            _captureStream.SetLength(0);
        }

        _innerStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _captureStream.Write(buffer, offset, count);
        _innerStream.Write(buffer, offset, count);
    }

    public event Action<byte[]> Captured;

    protected virtual void OnCaptured()
    {
        Captured(_captureStream.ToArray());
    }
}

public class Class1
{
    private string AField;
    private bool ASwitch;

    public void Method(IContext aContext)
    {
        var captureHandlerIsAttached = false;

        try
        {
            if (!ASwitch)
                return;

            Console.WriteLine(AField);

            if (!HasUrl(aContext))
            {
                return;
            }

            var response = aContext.HttpContext.Response;
            var captureStream = new CaptureStream(null);
            response.Filter = captureStream;

            captureStream.Captured += (output) => {
                try
                {
                    if (response.StatusCode != 200)
                    {
                        Console.WriteLine(AField);
                        return;
                    }

                    Console.WriteLine(aContext.HttpContext.Request.Url.AbsolutePath);
                }
                finally
                {
                    ReleaseTheLock();
                }
            };

            captureHandlerIsAttached = true;
        }
        finally
        {
            if (!captureHandlerIsAttached)
                ReleaseTheLock();
        }
    }

    private void ReleaseTheLock()
    {
        if (AField != null && Monitor.IsEntered(AField))
        {
            Monitor.Exit(AField);
            AField = null;
        }
    }

    protected virtual bool HasUrl(IContext filterContext)
    {
        if (filterContext.HttpContext.Request.Url == null)
        {
            return false;
        }
        return true;
    }
}
");
        }
    }
}
