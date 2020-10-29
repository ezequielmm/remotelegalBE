namespace PrecisionReporters.Platform.Domain.Commons
{
    public class EmailVerifyHtmlTemplate
    {
        public string GetVerifyTemplate(EmailTemplateInfo emailTemplateInfo)
        {
            var data = emailTemplateInfo.TemplateData.ToArray();
            return @$"<!DOCTYPE html>
                        <html>

                        <head>
                          <meta charset='utf-8'>
                          <meta name='viewport' content='width=device-width'>
                          <meta http-equiv='X-UA-Compatible' content='IE=edge'>
                          <meta name='x-apple-disable-message-reformatting'>
                          <link href='https://fonts.googleapis.com/css2?family=Lato:wght@400;700&family=Merriweather:wght@300&display=swap' rel='stylesheet'>
                          <title></title>

                          <!-- CSS -->
                          <style>
                            /* CLIENT-SPECIFIC STYLES */

                            /* Prevent WebKit and Windows mobile changing default text sizes */
                            body,
                            table,
                            td,
                            a {{
                              -webkit-text-size-adjust: 100%;
                              -ms-text-size-adjust: 100%;
                            }}

                            /* Remove spacing between tables in Outlook 2007 and up */
                            table,
                            td {{
                              mso-table-lspace: 0pt;
                              mso-table-rspace: 0pt;
                            }}

                            /* Allow smoother rendering of resized image in Internet Explorer */
                            img {{
                              -ms-interpolation-mode: bicubic;
                            }}

                            /* RESET STYLES */
                            img {{
                              border: 0;
                              height: auto;
                              line-height: 100%;
                              outline: none;
                              text-decoration: none;
                            }}

                            table {{
                              border-collapse: collapse !important;
                            }}

                            body {{
                              height: 100% !important;
                              margin: 0 !important;
                              padding: 0 !important;
                              width: 100% !important;
                            }}

                            /* Centers email on Android. */
                            div[style*='margin: 16px 0'] {{
                              margin: 0 !important;
                              font-size: 100% !important;
                            }}

                            /* Removing blue links in Apple Mail */
                            a[x-apple-data-detectors] {{
                              color: inherit !important;
                              text-decoration: none !important;
                              font-size: inherit !important;
                              font-family: inherit !important;
                              font-weight: inherit !important;
                              line-height: inherit !important;
                            }}

                            a:-webkit-any-link {{
                            cursor: pointer;
                            }}

                            @media screen and (max-width: 480px) {{
                              .container__fluid {{
                                display: block;
                                width: 100% !important;
                              }}
                            }}

                            .button-gold--a:hover {{
                              background: #ccb078 !important;
                              border-color: #ccb078 !important;
                            }}/* Button primary gold*/


                          </style>
                          <!--[if gte mso 9]>
                            <style type=”text/css”>
                              .fallback-font {{
                              font-family: Arial, sans-serif;
                              }}
                            </style>
                            <xml>
                                <o:OfficeDocumentSettings>
                                    <o:AllowPNG/>
                                    <o:PixelsPerInch>96</o:PixelsPerInch>
                                </o:OfficeDocumentSettings>
                            </xml>
                          <![endif]-->
                        </head>

                        <body style='margin: 0 !important; padding: 0 !important;'>
                            <table align='center' cellpadding='0' cellspacing='0' border='0' width='100%'>
                            <tr>
                                <td align='center' valign='top'>
                                <!--[if mso]>
                                <table cellspacing='0' cellpadding='0' border='0' width='600' align='center'>
                                <tr>
                                <td>
                                <![endif]-->

                                <!-- Email Body : BEGIN -->
                                <table align='center' cellspacing='0' cellpadding='0' border='0' width='100%' style='max-width: 450px; width: 100%;' class='container__fluid'>
                                    <tr>
                                    <td align='center' height='100%' valign='top' width='100%'>
                                        <!--[if (gte mso 9)|(IE)]>
                                        <table align='center' border='0' cellspacing='0' cellpadding='0' width='600'>
                                        <tr>
                                        <td align='center' valign='top' width='600'>
                                        <![endif]-->
                                        <table align='center' border='0' cellpadding='0' cellspacing='0' width='95%' style='max-width:600px;' bgcolor='#ffffff'>
                                            <tr>
                                            <td align='left' valign='top' style='font-size:14px;padding-top: 10%;padding-bottom: 10%;'>
                                                <span style='font-family: Merriweather, Georgia, “Times New Roman”, serif;font-weight: 400; font-size: 24px; line-height: 30px; color: #14232E;'>
                                                Verify your account to finish sign up for Remote Legal
                                                </span>
                                                <br><br><br>
                                                <span style='font-family: Lato, Trebuchet, “Trebuchet MS”, sans-serif; font-size: 16px; line-height: 24px; color: #666666;'>
                                                Hi <b style='color: #122D52;'>{data[0]}</b>
                                                </span>
                                                <br><br>
                                                <span style='font-family: Lato, Trebuchet, “Trebuchet MS”, sans-serif; font-size: 16px; line-height: 24px; color: #666666;'>
                                                Thanks for joining Remote Legal.
                                                </span>
                                                <br><br>
                                                <span style='font-family: Lato, Trebuchet, “Trebuchet MS”, sans-serif; font-size: 16px; line-height: 24px; color: #666666;'>
                                                Please confirm if <span style='color: #122D52;'>{data[1]}</span> is your email address by clicking the button bellow.
                                                </span>
                                                <br><br>
                                                <div style='display:inline-block;'>
                                                <!--[if mso]> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' arcsize='12%' stroke='f' fillcolor='#C09853' style='height:55px;v-text-anchor:middle;width:255.39px;' > <w:anchorlock/> <center style='width:100%;' > <![endif]-->
                                                <a href='{data[2]}' target='_blank' class='button-gold--a' style='box-sizing:content-box;background-color:#C09853;border-radius:12px;color:#FFF;display:inline-block;font-family: Lato, Arial, sans-serif;font-size:14px;font-style:normal;font-weight:normal;line-height:50px;text-align:center;text-decoration:none;padding: 0 90px; max-width: 100%; white-space: nowrap; -webkit-text-size-adjust:none;word-break:break-all;'>VERIFY ACCOUNT</a>
                                                <!--[if mso]> </center> </v:roundrect> <![endif]-->
                                                </div>
                                                <br><br>
                                                <span style='font-family: Lato, Trebuchet, “Trebuchet MS”, sans-serif; font-size: 16px; line-height: 24px; color: #666666;'>
                                                Need help? ask at <a href='{data[3]}' target='_blank' style='text-decoration: none; color: #C09853;'>{data[3]}</a>
                                                </span>

                                            </td>
                                            </tr>
                                        </table>
                                        <!--[if (gte mso 9)|(IE)]>
                                        </td>
                                        </tr>
                                        </table>
                                        <![endif]-->
                                    </td>
                                    </tr>
                                </table>
                                <!-- Email Body : END -->

                                <!--[if mso]>
                                </td>
                                </tr>
                                </table>
                                <![endif]-->
                                </td>
                            </tr>
                            </table>
                        </body>

                        </html>";
        }
    }
}
