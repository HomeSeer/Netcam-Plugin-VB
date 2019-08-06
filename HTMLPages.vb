Imports System.Text
Module HTMLPages
    Function BuildPage(sPage As String, sPageTitle As String)
        Dim sHTML As New StringBuilder
        sPage = sPage.ToLower
        Select Case sPage
            Case "editcameras"
                InitPage(sHTML)
                BuildHeader(sHTML, sPage, sPageTitle)
                BuildBody(sHTML, sPage) '<-- AJAX functions are in here
                ClosePage(sHTML)
        End Select

        Return sHTML.ToString
    End Function

    Sub InitPage(ByRef sHTML As StringBuilder)

        sHTML.Append("<!DOCTYPE html>")
        sHTML.Append("<html lang = ""en"" >")

    End Sub

    Sub BuildHeader(ByRef sHTML As StringBuilder, sPage As String, sPageTitle As String)

        sHTML.Append("<head>")
        sHTML.Append("<meta charset = ""utf-8"" >")
        sHTML.Append("<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">")
        sHTML.Append("<!--This maintains the scale of the page based on the scale of the screen-->")
        sHTML.Append("<meta name=""viewport"" content=""width=device-width, initial-scale=1"">")
        sHTML.Append("<meta name=""author"" content=""HomeSeer Technologies"">")
        sHTML.Append("<!--This liquid tag loads all of the necessary css files for HomeSeer-->")
        sHTML.Append("{{includefile '/bootstrap/css/page_common.css'}}")
        sHTML.Append("<link href=""../bootstrap/css/steppers.min.css"" rel=""stylesheet"">")
        sHTML.Append("<title>" & sPageTitle & "</title>")
        sHTML.Append("</head>")

    End Sub

    Sub BuildBody(ByRef sHTML As StringBuilder, sPage As String)
        'init the html body
        sHTML.Append("<body Class=""body homeseer-skin"">")
        sHTML.Append("<!--These liquid tags add the HomeSeer header And navbar to the top of the page when appropriate-->")
        sHTML.Append("    {{includefile 'header.html'}}")
        sHTML.Append("    {{includefile 'navbar.html'}}")
        sHTML.Append("<!--Primary container for the page content")

        sHTML.Append("    The .container class ensures the page content Is fit And centered to the screen-->")
        'Open the container div and user list
        sHTML.Append(" < div Class=""container"">")
        sHTML.Append("        <!-- MDB Steppers -->")
        sHTML.Append(" < ul id = ""process-stepper"" Class=""stepper linear"">")

        'Add a step
        sHTML.Append(" < li Class=""step active"">")
        sHTML.Append(" < div data-Step-label=""About this page"" Class=""step-title waves-effect waves-dark"">Step 1</div>")
        sHTML.Append(" < div Class=""step-New-content"" style=""display: block;"">")
        sHTML.Append("                    This Is a sample feature page that demonstrates how a guided process should be included with a plugin.  This Is very useful when there Is a clear set of steps that a user must follow in order to accomplish a given task.  For example: If a Then user needs To connect a New device To their HomeSeer system, they must follow a certain process, which they should be walked through Using a feature page such As this.")
        sHTML.Append(" < div Class=""step-actions"">")
        sHTML.Append(" < button id = ""btnStep1"" Class=""waves-effect waves-dark btn btn-sm btn-primary next-step"">Continue</button>")
        sHTML.Append("                    </div>")
        sHTML.Append("                </div>")
        sHTML.Append("            </li>")

        'Add a step
        sHTML.Append(" < li Class=""step"">")
        sHTML.Append(" < div data-Step-label=""Collect information"" Class=""step-title waves-effect waves-dark"">)Step 2</div>")
        sHTML.Append(" < div id = ""step2"" Class=""step-New-content"">")
        sHTML.Append("                    You can collect information that you can Then validate Using javascript To determine If the user can proceed With the process Or Not.")
        sHTML.Append(" < div Class=""row"">")
        sHTML.Append(" < div Class=""md-form col-12 ml-auto"" style=""margin-top:  16px;"">")
        sHTML.Append(" < Input() id = ""information-input"" type=""text"" Class=""form-control"">")
        sHTML.Append(" < label id = ""information-input-label"" For=""information-input"">Badger badger badger badger</label>")
        sHTML.Append("                        </div>")
        sHTML.Append("                    </div>")
        sHTML.Append(" < div Class=""step-actions"" style=""margin-top:  32px;"">")
        sHTML.Append(" < button Class=""waves-effect waves-dark btn btn-sm btn-secondary previous-step"">BACK</button>")
        sHTML.Append(" < button Class=""waves-effect waves-dark btn btn-sm btn-primary next-step"" data-feedback=""step2ValidationFunction"">CONTINUE</button>")
        sHTML.Append("                    </div>")
        sHTML.Append("                </div>")
        sHTML.Append("            </li>")

        'Add a step
        sHTML.Append(" < li Class=""step"">")
        sHTML.Append(" < div data-Step-label=""Select an item"" Class=""step-title waves-effect waves-dark"">Step 3</div>")
        sHTML.Append(" < div Class=""step-New-content"">")
        sHTML.Append("                    Sometimes, you may need the user To Select an Option from a list that Is dynamically driven by the state Of the plugin.  This can be done Using Liquid tags.  A Liquid tag calls a method In your plugin To inflate a section Of HTML.  This Select list was provided by the plugin method GetSampleSelectList()")
        sHTML.Append("                    {{plugin_function 'HSPI_NetCam' 'GetSampleSelectList' []}}")
        sHTML.Append("                    <!-- EXAMPLE")
        sHTML.Append(" <Select Class=""mdb-select md-form"" id=""step3SampleSelectList"">")
        sHTML.Append(" <option value = """" disabled selected>Color</Option>")
        sHTML.Append(" <option value = ""0"" > Red</Option>")
        sHTML.Append(" <option value = ""1"" > Orange</Option>")
        sHTML.Append(" <option value = ""2"" > Yellow</Option>")
        sHTML.Append(" <option value = ""3"" > Green</Option>")
        sHTML.Append(" <option value = ""4"" > Blue</Option>")
        sHTML.Append(" <option value = ""5"" > Indigo</Option>")
        sHTML.Append(" <option value = ""6"" > Violet</Option>")
        sHTML.Append("                    </select>")
        sHTML.Append(" - ->")
        sHTML.Append(" < div Class=""step-actions"">")
        sHTML.Append(" < button Class=""waves-effect waves-dark btn btn-sm btn-secondary previous-step"">BACK</button>")
        sHTML.Append(" < button Class=""waves-effect waves-dark btn btn-sm btn-primary next-step"">CONTINUE</button>")
        sHTML.Append("                    </div>")
        sHTML.Append("                </div>")
        sHTML.Append("            </li>")

        'Add a step
        sHTML.Append(" < li Class=""step"">")
        sHTML.Append(" < div data-Step-label=""Configure the interface"" Class=""step-title waves-effect waves-dark"">Step 4</div>")
        sHTML.Append(" < div Class=""step-New-content"">")
        sHTML.Append("                    You can also Call into your plugin To initiate a more complex process that requires the user To interact With a peripheral device so that they can Continue.  A great example Of this Is starting the process To add a New device To a Z-Wave network And Then having To put the device into inclusion mode so that the process can Continue.")
        sHTML.Append(" < div Class=""step-actions"">")
        sHTML.Append(" < button Class=""waves-effect waves-dark btn btn-sm btn-secondary previous-step"">BACK</button>")
        sHTML.Append(" < button Class=""waves-effect waves-dark btn btn-sm btn-primary next-step"" data-feedback=""step4FeedbackFunction"">CONTINUE</button>")
        sHTML.Append("                    </div>")
        sHTML.Append("                </div>")
        sHTML.Append("            </li>")

        'Add a step
        sHTML.Append(" < li Class=""step"">")
        sHTML.Append(" < div Class=""step-title waves-effect waves-dark"">Step 5</div>")
        sHTML.Append(" < div id = ""lastStep"" Class=""step-New-content"">")
        sHTML.Append(" < p id = ""lastStepText"" >")
        sHTML.Append("                        Finished! </p>")
        sHTML.Append(" < div Class=""step-actions"">")
        sHTML.Append(" < button Class=""waves-effect waves-dark btn btn-sm btn-primary m-0 mt-4"" onclick=""finish()"" type= ""button"">Finish</button>")
        sHTML.Append("                    </div>")
        sHTML.Append("                </div>")
        sHTML.Append("            </li>")

        'Close the container div and user list
        sHTML.Append("        </ul>")
        sHTML.Append("    </div>")

        'Put in the corresponding AJAX functionality
        BuildAJAX(sHTML, sPage)

        'close the body tag
        sHTML.Append("</body>")

    End Sub

    Sub BuildAJAX(ByRef sHTML As StringBuilder, sPage As String)

        'Init the AJAX
        sHTML.Append("<!-- Bootstrap core JavaScript")
        sHTML.Append(" ================================================== -->")
        sHTML.Append("<!-- Placed at the end of the document so the pages load faster -->")
        sHTML.Append("{{includefile 'bootstrap/js/page_common.js'}}")
        sHTML.Append(" < script type = ""text/javascript"" src=""../bootstrap/js/steppers.min.js""></script>")
        sHTML.Append(" < script type = ""text/JavaScript"" >")
        sHTML.Append("$(document).ready(Function() {")
        sHTML.Append("$('.stepper').mdbStepper();")
        sHTML.Append("})")

        'Put in your page specific AJAX functions
        Select Case sPage
            Case "editcameras"
                sHTML.Append("Function step1ValidationFunction() {")
                sHTML.Append("var informationLabel = $('#information-input-label');")
                sHTML.Append("var informationErrorLabel = $('#information-input-error');")
                sHTML.Append("var informationInput = $('#information-input');")

                sHTML.Append("If (informationInput[0].value == null || informationInput[0].value.trim() == """") {")
                sHTML.Append("var step2 = informationLabel.parent();")
                sHTML.Append("If (informationErrorLabel.length == 0) Then {")
                sHTML.Append("step2.append('<label id=""information-input-error"" for=""information-input"" class=""invalid"">This field is required</label>');")
                sHTML.Append("}")
                sHTML.Append("$('#process-stepper').destroyFeedback();")
                sHTML.Append("Return;")
                sHTML.Append("}")

                sHTML.Append("informationErrorLabel.remove();")
                sHTML.Append("$('#process-stepper').nextStep();")
                sHTML.Append("Return;")
                sHTML.Append("}")
        End Select

        'Add the PostBack AJAX function (this uses the name you passed as the page name SO THEY MUST MATCH)
        sHTML.Append("Function PostBackFunction() {")
        sHTML.Append("var informationInput = $('#information-input')[0];")
        sHTML.Append("var selection = $('#step3SampleSelectList')[0];")
        sHTML.Append("var internalData = {textValue: informationInput.value, colorIndex: selection.value};")
        sHTML.Append("var jsonData = JSON.stringify(internalData);")

        sHTML.Append("$.ajax({")
        sHTML.Append("type:  ""POST"",")
        sHTML.Append("async:  ""true"",")
        sHTML.Append("url '/" & _plugin.Id & "/" & sPage & ".html',") '<---here is the page name
        sHTML.Append("cache: false,")
        sHTML.Append("data: jsonData,")
        sHTML.Append("success: Function(response){")

        sHTML.Append("If (response == ""Error"") Then {")
        sHTML.Append("$('#process-stepper').destroyFeedback();")
        sHTML.Append("alert(""Error"");")
        sHTML.Append("Return;")
        sHTML.Append("}")
        sHTML.Append("ElseIf (response.startsWith('<')) {")
        sHTML.Append("$('#process-stepper').destroyFeedback();")
        sHTML.Append("alert(""Error"");")
        sHTML.Append("Return;")
        sHTML.Append("}")
        sHTML.Append("ElseIf (response.startsWith('{')) {")
        sHTML.Append("var responseObj = JSON.parse(response);")
        sHTML.Append("var mystEle = '<img src=\""' + responseObj.data + '\""/>';")
        sHTML.Append("$('#lastStep').prepend(mystEle);")
        sHTML.Append("$('#lastStepText').text("""");")
        sHTML.Append("$('#process-stepper').nextStep();")
        sHTML.Append("Return;")
        sHTML.Append("}")

        sHTML.Append("$('#lastStepText').text(response);")
        sHTML.Append("$('#process-stepper').nextStep();")
        sHTML.Append("},")
        sHTML.Append("Error: Function(){")
        sHTML.Append("$('#process-stepper').destroyFeedback();")
        sHTML.Append("alert(""Error"");")
        sHTML.Append("}")
        sHTML.Append("});")
        sHTML.Append("}")

        sHTML.Append("Function finish() {")
        sHTML.Append("var devicesPage = window.location.origin + ""/devices.html"";")
        sHTML.Append("window.location.assign(devicesPage);")
        sHTML.Append("}")
        sHTML.Append("</script>")
    End Sub

    Sub ClosePage(ByRef sHTML As StringBuilder)
        sHTML.Append("</html>")
    End Sub
End Module