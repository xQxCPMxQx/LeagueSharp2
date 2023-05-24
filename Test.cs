using Microsoft.AspNetCore.Mvc;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using ENKA.Denetim.CRM.Service.Abstract;
using ENKA.Denetim.CRM.Entity.Model;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using ENKA.Denetim.CRM.UI.Controllers;
using ENKA.Denetim.CRM.Service;
using System.Drawing;
using System.Globalization;
using SelectPdf;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using ENKA.Denetim.CRM.Models;

namespace ENKA.Denetim.CRM.UI.Helpers
{

    public static class PdfHelper
    {


        public static ServiceResult<AuditSchedule> DownloadPDF([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService,
            [FromServices] IAuditorsTypeMappingService _auditorsTypeMappingService,
            [FromServices] IAuditScheduleService _auditScheduleService,
            [FromServices] IAuditCardsService _auditCardsService,
            [FromServices] IScheduleTypeService _scheduleTypeService,
            [FromServices] IAuditScopesAndCriteriaService _auditScopesAndCriteriaService, IWebHostEnvironment _env,
            int[] IdList, string JWToken)

        {
            var result = new ServiceResult<AuditSchedule>();
            try
            {
                // SetLicenseExample(_env.WebRootPath);

                IDictionary<string, string> mailContents = new Dictionary<string, string>();

                string html = File.ReadAllText("wwwroot/PdfTemplate/pdfNew.html");
                //Id listesi gelen auditschedule'ların service üzerinden listeye alınması
                List<AuditSchedule> auditScheduleList = new List<AuditSchedule>();
                List<UatUserDetail> user = new List<UatUserDetail>();
                List<string> userMail = new List<string>();
                List<string> userMailCC = new List<string>();
                List<UatUserDetail> Leaduser = new List<UatUserDetail>();

                foreach (var id in IdList)
                {
                    auditScheduleList.Add(_auditScheduleService.GetByID(id).Data);
                }


                foreach (var auditSchedule in auditScheduleList)
                {
                    string userList = "";
                    string auditorList = "";
                    string LeaduserList = "";
                    var idss = (Array.ConvertAll(auditSchedule.AuditScopesIDs.Split(",").ToArray(), s => int.Parse(s)));
                    var idsCriteria = (Array.ConvertAll(auditSchedule.AuditCriteriaIDs.Split(",").ToArray(),
                        s => int.Parse(s)));
                    auditSchedule.ScopeIds = idss;
                    auditSchedule.CriteriaIds = idsCriteria;
                    var auditorsTypeMapping =
                        _auditorsTypeMappingService.GetByTemplateID(auditSchedule.AuditScheduleID).Data;
                    if (auditorsTypeMapping.Count > 0)
                    {
                        foreach (var item in auditorsTypeMapping)
                        {
                            item.UserID = Array.ConvertAll(item.UserIDs.Split(",").ToArray(), s => int.Parse(s));
                            foreach (var itemUserIDs in item.UserID)
                            {
                                var users = _uatService.UserDetailById(JWToken, itemUserIDs).Data;
                                user.Add(users);
                                if (item.AuditorsTypeID == (int)AuditorEnum.LeadAuditor)
                                {
                                    //if (!string.IsNullOrEmpty(auditorList))
                                    //{
                                    LeaduserList += ", " + users.FullName;

                                    //}
                                    //else
                                    //    LeaduserList += users.FullName;
                                }
                                else
                                {
                                    //if (!string.IsNullOrEmpty(auditorList))
                                    //{
                                    auditorList += ", " + users.FullName;
                                    //}
                                    //else
                                    //    auditorList += users.FullName;


                                }
                            }
                        }
                    }

                    LeaduserList = LeaduserList.Substring(2, LeaduserList.Length - 2);
                    auditorList = auditorList.Substring(2, auditorList.Length - 2);

                    string[] userTo = auditSchedule.ToAudit.Split(", ");
                    if (userTo.Length > 0)
                    {
                        foreach (var to in userTo)
                        {
                            userMail.Add(to);
                        }
                    }

                    string[] userCC = auditSchedule.CcAudit.Split(", ");
                    foreach (var cc in userCC)
                    {
                        if (!string.IsNullOrEmpty(cc))
                            userMailCC.Add(cc);

                    }

                    foreach (var property in typeof(AuditSchedule).GetProperties())
                    {
                        var value = property.GetValue(auditSchedule) == null
                            ? ""
                            : property.GetValue(auditSchedule).ToString();

                        html = html.Replace("{" + property.Name.ToString() + "}", value);
                    }

                    ScheduleType type = _scheduleTypeService.GetByID(auditSchedule.ScheduleTypeID).Data;
                    List<UatUser> allUser = _uatService.UserList(JWToken).Data.data;
                    AuditCards auditCard = _auditCardsService.GetByID(auditSchedule.AuditCardID).Data;
                    List<AuditScopesAndCriteria> scopesAndCriterias = new List<AuditScopesAndCriteria>();
                    if (auditSchedule.ScopeIds != null)
                    {
                        foreach (var id in auditSchedule.ScopeIds)
                        {
                            scopesAndCriterias.Add(_auditScopesAndCriteriaService.GetByID(id).Data ??
                                                   new AuditScopesAndCriteria());
                        }
                    }

                    if (auditSchedule.CriteriaIds != null)
                    {
                        foreach (var id in auditSchedule.CriteriaIds)
                        {
                            scopesAndCriterias.Add(_auditScopesAndCriteriaService.GetByID(id).Data ??
                                                   new AuditScopesAndCriteria());
                        }
                    }

                    AuditScopesAndCriteria scope =
                        scopesAndCriterias.FirstOrDefault(q => q.Type == (int)ScopesAndCriteriasEnum.Scope);
                    AuditScopesAndCriteria criteria =
                        scopesAndCriterias.FirstOrDefault(q => q.Type == (int)ScopesAndCriteriasEnum.Criteria);
                    List<AuditScopesAndCriteria> scopes = scopesAndCriterias
                        .Where(q => q.Type == (int)ScopesAndCriteriasEnum.Scope).ToList();
                    List<AuditScopesAndCriteria> criterias = scopesAndCriterias
                        .Where(q => q.Type == (int)ScopesAndCriteriasEnum.Criteria).ToList();
                    //string scopeMail = "";
                    //string criteriaMail = "";
                    if (scopes != null)
                    {
                        string scopeTitle = string.Join("<br/>", scopes.Select(q => q.Title));
                        ;
                        //foreach (var sc in scopes)
                        //{
                        //    //scopeMail += @$"<a href=""{sc.ResourceLink}""><p style=""padding-top: 5pt; padding-left: 5pt; text-indent: 0pt; text-align: justify;"">{sc.Title}</p></a>";
                        //    scopeTitle += sc.Title + " ,";
                        //}
                        html = html.Replace("{Scope.Title}", scopeTitle);

                    }

                    string criteriaTitle = "";
                    if (criterias != null)
                    {
                        criteriaTitle = string.Join("<br>", criterias.Select(q => q.Title));
                        //foreach (var cr in criterias)
                        //{
                        //    //criteriaMail += @$"<a href=""{cr.ResourceLink}""><p style=""padding-top: 5pt; padding-left: 5pt; text-indent: 0pt; text-align: justify;"">{cr.Title}</p></a>";
                        //    criteriaTitle += cr.Title + " ,";
                        //}
                        html = html.Replace("{Criterias.Title}", criteriaTitle);
                    }
                    //if (scopes != null)
                    //{

                    //    foreach (var sc in scopes)
                    //    {
                    //        scopeTitle += @$"<p style=""padding-top: 5pt; padding-left: 5pt; text-indent: 0pt; text-align: justify;"">{sc.Title}</p>";
                    //    }
                    //    html = html.Replace("{Scope.Title}", scopeTitle);
                    //}
                    //if (criterias != null)
                    //{

                    //    foreach (var cr in criterias)
                    //    {
                    //        criteriaTitle += @$"<p style=""padding-top: 5pt; padding-left: 5pt; text-indent: 0pt; text-align: justify;"">{cr.Title}</p>";

                    //    }
                    //    html = html.Replace("{Criterias.Title}", criteriaTitle);
                    //}

                    //string auditorName = "";

                    //if (user != null)
                    //{
                    //    foreach (var item in user)
                    //    {
                    //        auditorName += @$"<p style=""color: black; font-family: Arial, sans-serif; font-style: italic; text-decoration: none; font-size: 12pt; padding-left: 5pt; text-indent: 0pt; text-align: left; "">{item.FullName}</p>";

                    //    }
                    //}
                    //if (Leaduser != null)
                    //{
                    //    string leadUserName = "";
                    //    foreach (var item in Leaduser)
                    //    {

                    //        leadUserName += @$"<p style=""color: black; font-family: Arial, sans-serif; font-style: italic; text-decoration: none; font-size: 12pt; padding-left: 5pt; text-indent: 0pt; text-align: left; "">{item.FirstName + " " + item.LastName}</p>";

                    //    }
                    //    html = html.Replace("{LeadAuditor}", leadUserName);
                    //}

                    html = html.Replace("{Auditors}", auditorList);
                    html = html.Replace("{LeadAuditor}", LeaduserList);
                    html = html.Replace("{ProjectCode}", auditCard.Name == null ? "" : auditCard?.Name);
                    html = html.Replace("{ScheduleType.Name}", type.Name == null ? "" : type.Name);
                    html = html.Replace("{PDFCreateDate}", DateTime.Now.ToString("dd-MM-yyyy"));
                    //string link = "https://audit.enka.com/audit-schedule-update/" + auditSchedule.AuditScheduleID;
                    string link = "https://audit.enka.com";
                    //string mailBody = "<p>Sayın yetkili,<br/>Audit Managemet sisteminde yeni bir Schedule kaydı " + @allUser.FirstOrDefault(q => q.UserID == auditSchedule.CreatedBy).FullName + " tarafından oluşturulmuştur. Schedule takibi için aşağıdaki linkten sisteme girebilirsiniz.</p><label>LINK :" + $" <a href=\"{link}\">GİRİŞ</a></label>";

                    string mailBody = "<p>Sayın " + auditSchedule.AuditMailName +
                                      ",</p><p>2023 ENKA İç Denetim Programı kapsamında, " + auditCard?.Name +
                                      " denetimini " + auditSchedule.PlannedStartDate.ToString("dd-MM-yyyy") + " ile " +
                                      auditSchedule.PlannedEndDate.ToString("dd-MM-yyyy") +
                                      " tarihleri arasında gerçekleştirmeyi planlıyoruz. Denetim kriterini, kapsamını, yöntemini, programını ve denetçilerin bilgilerini içeren denetim bildirimini ekte bulabilirsiniz.</p><p> İlgili denetimde süreç, raporlama ve denetim sonrası bulgu takibi için hem denetim hem proje ekibi tarafından ENKA AMS Denetim Yönetim Sistemi kullanılacaktır. Denetim programına ve AMS Sistemine ilişkin sorularınız olması halinde Denetim Ekibi ile iletişime geçebilirsiniz.</p><p>Sizlerden ricamız; denetimin planlandığı şekilde yürütülebilmesi için gerekli kaynakların ayrılmasını sağlayabilirseniz çok memnun oluruz. </p>";
                    //+ 
                    //$@"<p style=""font-size:20px;""><strong>Denetim Planı Duyurusu</strong></p><hr/<p><strong>AuditRefNumber:
                    //</strong> {auditCard.ProjectCode}</p><p><strong>PlannedStartDate:</strong> {auditSchedule.PlannedStartDate.ToString("dd-MM-yyyy")}</p>
                    //<p><strong>AuditObjectives:</strong> {auditSchedule.AuditObjectives}</p><p><strong>AuditMethodology:</strong> {auditSchedule.AuditMethodology}</p>
                    //<p><strong>AuditOpeningMeeting:</strong> {auditSchedule.AuditOpeningMeeting}</p><p><strong>Auditors:</strong> {auditorName}</p>
                    //<p><strong>Scopes:</strong>{scopeMail}</p><p><strong>Criterias:</strong> {criteriaMail}</p>";

                    mailBody += "<strong><p>Saygılarımızla,</br> ENKA İç Denetim Ekibi</p></strong>";


                    mailBody +=
                        "<p>Bu bilgilendirme e-postası Audit Management System tarafından otomatik olarak iletilmiştir.</br>(For English, please see below.)</p>";

                    mailBody +=
                        "<p>--------------------------------------------------------------------------------------------------------------------------------</p>";

                    mailBody += "<p>Dear " + auditSchedule.AuditMailName +
                                ",</p><p>Within the scope of the 2023 ENKA Internal Audit Program, we plan to conduct the " +
                                auditCard?.Name + " on-site audit between " +
                                auditSchedule.PlannedStartDate.ToString("dd-MM-yyyy") + " and " +
                                auditSchedule.PlannedEndDate.ToString("dd-MM-yyyy") +
                                " You can access the audit notification document, which includes audit criteria, scope, method, program and auditor information, from the attached file. </br></br>ENKA AMS Audit Management System will be used by both the audit and the project team for the audit process, reporting and post-audit finding tracking. If you have questions regarding the audit program and the AMS system, you can contact the Audit Team.</br></br>It would be greatly appreciated if you could ensure that the necessary resources are allocated so that the audit can be carried out as planned.</p>";

                    mailBody += "<strong><p>Regards,</br>ENKA Internal Audit Team</p></strong>";

                    mailBody +=
                        "<p>This notification e-mail has been sent automatically by the Audit Management System.</p>";


                    string newPdfPath = _env.WebRootPath + @"\Pdfs\";
                    string newPdfPathTemplate = _env.WebRootPath + @"\PdfTemplate\";

                    //MemoryStream ms = new MemoryStream();


                    //HtmlLoadOptions options = new HtmlLoadOptions(newPdfPathTemplate);
                    //Document pdfDocument = new Document(newPdfPathTemplate+"pdf.html");
                    //pdfDocument.Save(newPdfPath+"outputPath.pdf");




                    //MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseFromServer));
                    //stream.Flush();
                    //stream.Position = 0;

                    //HtmlLoadOptions options = new HtmlLoadOptions(newPdfPathTemplate);


                    //// Load HTML file
                    //Document pdfDocument = new Document(stream, options);

                    //options.PageInfo.IsLandscape = true;
                    //// Save output as PDF format
                    //pdfDocument.Save(newPdfPath + "ece.pdf");


                    //var Renderer = new IronPdf.ChromePdfRenderer();
                    //var config = new PdfGenerateConfig();
                    //config.PageOrientation = PdfSharp.PageOrientation.Landscape;
                    //config.ManualPageSize = new PdfSharp.Drawing.XSize(1132, 842);
                    //config.SetMargins(10);
                    //Byte[] res = null;
                    //var pdf = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(html.ToString(), config);
                    //pdf.Save(ms);
                    //res = ms.ToArray();
                    //pdf.Dispose();
                    //ms.Close();
                    SelectPdf.HtmlToPdf htmlToPdf = new SelectPdf.HtmlToPdf();
                    htmlToPdf.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                    htmlToPdf.Options.MarginLeft = 15;
                    htmlToPdf.Options.MarginRight = 15;
                    htmlToPdf.Options.EmbedFonts = true;

                    SelectPdf.PdfDocument pdfDocument = htmlToPdf.ConvertHtmlString(html);
                    SelectPdf.PdfFont font = pdfDocument.AddFont(PdfStandardFont.Helvetica);
                    pdfDocument.Fonts.Add(font);
                    byte[] pdf = pdfDocument.Save();

                    pdfDocument.Close();
                    File.WriteAllBytes(@"wwwroot\Pdfs\" + auditSchedule.AuditRefNumber + ".pdf", pdf);
                    //using var pdfHtml = Renderer.RenderHtmlAsPdf(html);
                    //pdfHtml.SaveAs(newPdfPath + "html.pdf");
                    //var document = new HTMLDocument(html.ToString());
                    //var options = new PdfSaveOptions();
                    //Converter.ConvertHTML(document, options, "output.pdf");
                    var pdfName = auditCard.Name + " Denetim Planı  " + auditSchedule.PlanedStartDateOnly + ".pdf";


                    if (auditSchedule.AuditPlanFile != null)
                    {
                        string outputPdfPath = _env.WebRootPath + @"\uploads\" + auditSchedule.AuditPlanFile;


                        //PdfFileEditor pdfeditor = new PdfFileEditor();
                        //// output stream
                        //FileStream outputstream = new FileStream(newPdfPath + pdfName, FileMode.Create);
                        //// input streams
                        //FileStream inputstream1 = new FileStream(newPdfPath + auditSchedule.AuditRefNumber + ".pdf", FileMode.Open);
                        //FileStream inputstream2 = new FileStream(outputPdfPath, FileMode.Open);
                        using (PdfSharp.Pdf.PdfDocument one =
                               PdfReader.Open(newPdfPath + auditSchedule.AuditRefNumber + ".pdf",
                                   PdfDocumentOpenMode.Import))
                        using (PdfSharp.Pdf.PdfDocument two = PdfReader.Open(outputPdfPath, PdfDocumentOpenMode.Import))
                        using (PdfSharp.Pdf.PdfDocument outPdf = new PdfSharp.Pdf.PdfDocument())
                        {
                            CopyPages(one, outPdf);
                            CopyPages(two, outPdf);
                            outPdf.Save(newPdfPath + pdfName);
                        }
                        //// merge files
                        //pdfeditor.Concatenate(inputstream1, inputstream2, outputstream);
                        //inputstream1.Dispose();
                        //inputstream2.Dispose();
                        //outputstream.Dispose();

                        //using IronPdf.PdfDocument inputPDFDocument = IronPdf.PdfDocument.FromFile(outputPdfPath);

                        //var pdf = IronPdf.PdfDocument.Merge(new IronPdf.PdfDocument(newPdfPath + "html.pdf"), new IronPdf.PdfDocument(outputPdfPath));
                        //pdf.SaveAs(newPdfPath + auditSchedule.AuditRefNumber + "-" + @type.Name + " Denetim Planı Duyurusu.pdf");
                    }


                    if (user != null && user.Count > 0)
                    {
                        foreach (var item in user)
                        {
                            userMailCC.Add(item.Email);
                        }
                    }

                    if (userMail.Count > 0)
                    {
                        _mailService.SendEmail(string.Join(",", userMail),
                            auditCard.Name + "- Denetim Planı / Audit Schedule", mailBody, newPdfPath + pdfName,
                            string.Join(",", userMailCC));
                    }

                    try
                    {
                        var mail = new Mail()
                        {
                            ToMail = string.Join(",", userMail),
                            Subject = mailBody,
                            AttachmentFilePath = newPdfPath + pdfName,
                            MailCC = string.Join(",", userCC),
                            Title = "[REMINDER!!]" + auditSchedule.AuditRefNumber + "-" + type.Name +
                                    " Denetim Planı Duyurusu",
                            PlannedStartDate = auditSchedule.PlannedStartDate
                        };

                        _mailService.Insert(mail);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                result.IsSuccess = true;

            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }

        public static void CopyPages(PdfSharp.Pdf.PdfDocument from, PdfSharp.Pdf.PdfDocument to)
        {
            for (int i = 0; i < from.PageCount; i++)
            {
                to.AddPage(from.Pages[i]);
            }
        }

        public static ServiceResult<AuditSchedule> ScopeCompleteMail([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService,
            [FromServices] IAuditorsTypeMappingService _auditorsTypeMappingService,
            [FromServices] IAuditScheduleService _auditScheduleService,
            [FromServices] IAuditCardsService _auditCardsService,
            [FromServices] IScheduleTypeService _scheduleTypeService,
            [FromServices] IAuditScopesAndCriteriaService _auditScopesAndCriteriaService, int scheduleId,
            [FromServices] PrincipalUserData currentUser, [FromServices] IFindingsService _findingsService,
            List<string> audScopes)

        {
            var result = new ServiceResult<AuditSchedule>();
            try
            {

                IDictionary<string, string> mailContents = new Dictionary<string, string>();
                //Main HTML kodu daha sonra dosya üzerinden çekilebilir.

                //Id listesi gelen auditschedule'ların service üzerinden listeye alınması
                AuditSchedule auditSchedule = new AuditSchedule();
                List<UatUserDetail> Leaduser = new List<UatUserDetail>();

                auditSchedule = _auditScheduleService.GetByID(scheduleId).Data;

                if (!string.IsNullOrEmpty(auditSchedule.AuditScopesIDs))
                {
                    var idss = (Array.ConvertAll(auditSchedule.AuditScopesIDs.Split(",").ToArray(), s => int.Parse(s)));
                    auditSchedule.ScopeIds = idss;
                }

                var auditorsTypeMapping =
                    _auditorsTypeMappingService.GetByTemplateID(auditSchedule.AuditScheduleID).Data;

                if (auditorsTypeMapping.Count > 0)
                {
                    foreach (var item in auditorsTypeMapping)
                    {
                        item.UserID = Array.ConvertAll(item.UserIDs.Split(",").ToArray(), s => int.Parse(s));
                        foreach (var itemUserIDs in item.UserID)
                        {
                            var users = _uatService.UserDetailById(currentUser.Token.AccessToken, itemUserIDs).Data;
                            if (item.AuditorsTypeID == (int)AuditorEnum.LeadAuditor)
                            {
                                Leaduser.Add(users);
                            }
                        }
                    }
                }

                string scopes = "";
                string scopeAndFinding = "";
                foreach (var item in audScopes)
                {
                    var scopeName = _auditScopesAndCriteriaService.GetByID(Convert.ToInt32(item)).Data;
                    var finding = _findingsService
                        .GetByScheduleAndScopeID(auditSchedule.AuditScheduleID, Convert.ToInt32(item)).Data;
                    if (string.IsNullOrEmpty(scopes))
                    {
                        scopes += scopeName.Title;

                    }
                    else
                    {
                        scopes += ", " + scopeName.Title;

                    }

                    scopeAndFinding += scopeName.Title.ToLower() + "-" + finding.Count + ", ";

                }
                #region DENETİM PLANI İLK BİLDİRİM MAİLİ 

                var auditorNameSurname =
                    _uatService.UserDetailById(currentUser.Token.AccessToken, currentUser.User.ID).Data;
                AuditCards auditCard = _auditCardsService.GetByID(auditSchedule.AuditCardID).Data;
                ScheduleType type = _scheduleTypeService.GetByID(auditSchedule.ScheduleTypeID).Data;
                //AuditScopesAndCriteria scopesAndCriterias = _auditScopesAndCriteriaService.GetByID(scopeID).Data;
                string link = "https://audit.enka.com/questions?id=" + auditSchedule.AuditScheduleID;
                string mailBody = "<p>Sayın Yetkili,<br/><br/> Aşağıda bilgisi verilen denetim bulguları, denetçi " +
                                  auditorNameSurname?.FullName +
                                  " tarafında sisteme girilmiş ve tamamlanmıştır. Onayınız beklenmektedir.<br/><br/>" +
                                  "<strong>Proje Adı:</strong> " + auditCard.Name + "<br/>" +
                                  "<strong>Denetim Ref. No:</strong> " + auditSchedule.AuditRefNumber + "<br/>" +
                                  "<strong>Denetim Tarihi:</strong> " +
                                  auditSchedule.PlannedStartDate.ToString("dd-MM-yyyy") + " - " +
                                  auditSchedule.PlannedEndDate.ToString("dd-MM-yyyy") + "<br/>" +
                                  "<strong>Tamamlanan Kapsam(lar):</strong> " + scopes.ToLower() + "<br/>"
                                  + "<strong> Bulgu Sayısı: </strong>" + scopeAndFinding +
                                  "<br/><br/> Sisteme giriş yapmak, bulguları onaylamak ve rapor oluşturmak için lütfen aşağıdaki bağlantıya tıklayınız:<br/>" +
                                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Saygılarımızla,<br/>ENKA AMS Yönetimi</p></strong>";

                mailBody +=
                    "<p>Bu bilgilendirme e-postası Audit Management System tarafından otomatik olarak iletilmiştir.</br>(For English, please see below.)</p>";

                mailBody +=
                    "<p>--------------------------------------------------------------------------------------------------------------------------------------------</p>";

                mailBody +=
                    "<p>Dear Responsible,<br/><br/> The internal audit findings, information of which are given below, were entered into the system and completed by the auditor " +
                    auditorNameSurname?.FullName + ". Your approval is awaited.<br/><br/>"

                    + "<strong>Project Name:</strong> " + auditCard.Name + "<br/>"
                    + "<strong>Audit Ref. No:</strong> " + auditSchedule.AuditRefNumber + "<br/>"
                    + "<strong>Audit Date:</strong> " + auditSchedule.PlannedStartDate.ToString("dd-MM-yyyy") +
                    auditSchedule.PlannedEndDate.ToString("dd-MM-yyyy") + "<br/>"
                    + "<strong>Completed Scopes):</strong> " + scopes.ToLower() + "<br/>"
                    + "<strong> Number of Findings: </strong>" + scopeAndFinding +
                    "<br/><br/>Please click on the link below to log in to the system, confirm the findings and generate an audit report:<br/>" +
                    $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Regards,<br/>ENKA AMS Management</p></strong>";

                mailBody +=
                    "<p>This notification e-mail has been sent automatically by the Audit Management System.</p>";

                #endregion

                if (Leaduser.Count > 0)
                {
                    foreach (var item in Leaduser)
                    {
                        _mailService.SendEmail(item.Email,
                            auditCard.Name +
                            " İç Denetimi – Bulgu Bilgilendirme & Onay / Internal Audit Findings Disclosure & Approval",
                            mailBody);

                    }
                }



                result.IsSuccess = true;

            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }


        public static ServiceResult<AuditReport> CreateReport([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService,
            [FromServices] IAuditorsTypeMappingService _auditorsTypeMappingService,
            [FromServices] IAuditScheduleService _auditScheduleService,
            [FromServices] IAuditCardsService _auditCardsService,
            [FromServices] IScheduleTypeService _scheduleTypeService,
            [FromServices] IAuditScopesAndCriteriaService _auditScopesAndCriteriaService,
            [FromServices] ICompleteScopeMappingService _completeScopeMappingService,
            [FromServices] IAuditReportService _auditReportService, [FromServices] IFindingsService _findingsService,
            [FromServices] IAuditScopesQuestionsService _auditScopesQuestionsService,
            [FromServices] IEmployeeService _employeeService, [FromServices] IBussinesLineService _businessLineService,
            AuditReport report, int scheduleId, string JWToken)

        {
            var result = new ServiceResult<AuditReport>();
            try
            {


                IDictionary<string, string> mailContents = new Dictionary<string, string>();
                //Main HTML kodu daha sonra dosya üzerinden çekilebilir.
                string html = File.ReadAllText("wwwroot/PdfTemplate/auditReportNew.html");

                //Id listesi gelen auditschedule'ların service üzerinden listeye alınması
                List<AuditSchedule> auditScheduleList = new List<AuditSchedule>();
                List<string> userMail = new List<string>();
                List<string> userMailCC = new List<string>();
                List<User> Leaduser = new List<User>();
                string userList = "";
                string auditorList = "";
                string LeaduserList = "";

                auditScheduleList.Add(_auditScheduleService.GetByID(scheduleId).Data);

                foreach (var auditSchedule in auditScheduleList)
                {
                    var idss = (Array.ConvertAll(auditSchedule.AuditScopesIDs.Split(",").ToArray(), s => int.Parse(s)));
                    var idsCriteria = (Array.ConvertAll(auditSchedule.AuditCriteriaIDs.Split(",").ToArray(),
                        s => int.Parse(s)));
                    auditSchedule.ScopeIds = idss;
                    auditSchedule.CriteriaIds = idsCriteria;
                    var auditorsTypeMapping =
                        _auditorsTypeMappingService.GetByTemplateID(auditSchedule.AuditScheduleID).Data;
                    if (auditorsTypeMapping.Count > 0)
                    {
                        foreach (var item in auditorsTypeMapping)
                        {
                            item.UserID = Array.ConvertAll(item.UserIDs.Split(",").ToArray(), s => int.Parse(s));
                            foreach (var itemUserIDs in item.UserID)
                            {
                                var users = _uatService.UserDetailById(JWToken, itemUserIDs).Data;
                                //users.RoleID = item.AuditorsTypeID;
                                userList +=
                                    @$"<tr><td style = ""width: 25%;""> {Enum.GetName(typeof(AuditorEnum), item.AuditorsTypeID)} </td><td style = ""width: 25%;""> {users.FullName} </td><td style = ""width: 25%;""> {users.Email} </td><td style = ""width: 25%;""> {auditSchedule.ActDate.ToString("dd-MM-yyyy") + " / " + auditSchedule.ActEndDate.ToString("dd-MM-yyyy")} </td></tr>";
                                if (item.AuditorsTypeID == (int)AuditorEnum.LeadAuditor)
                                {
                                    if (!string.IsNullOrEmpty(auditorList))
                                        LeaduserList += "," + users.FullName;
                                    else
                                        LeaduserList += users.FullName;
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(auditorList))
                                        auditorList += "," + users.FullName;
                                    else
                                        auditorList += users.FullName;
                                }
                            }
                        }
                    }

                    string[] userTo = auditSchedule.ToAudit.Split(",");
                    foreach (var to in userTo)
                    {
                        userMail.Add(to);
                    }

                    string[] userCC = auditSchedule.CcAudit.Split(",");
                    foreach (var cc in userCC)
                    {
                        userMailCC.Add(cc);
                    }

                    //foreach (var property in typeof(AuditSchedule).GetProperties())
                    //{
                    //    var value = property.GetValue(auditSchedule) == null ? "" : property.GetValue(auditSchedule).ToString();
                    //    if (html.Contains("{" + property.Name.ToString() + "}") && !string.IsNullOrEmpty(value))
                    //    {
                    //        mailContents.Add(property.Name, value);
                    //    }
                    //    html = html.Replace("{" + property.Name.ToString() + "}", value);
                    //}

                    ScheduleType type = _scheduleTypeService.GetByID(auditSchedule.ScheduleTypeID).Data;
                    ScheduleType Parent = _scheduleTypeService.GetByID(type.ParentID).Data;
                    AuditCards auditCard = _auditCardsService.GetByID(auditSchedule.AuditCardID).Data;
                    BussinesLine auditMethod = _businessLineService.GetByID(auditSchedule.AuditMethodID).Data;
                    List<BussinesLine> businessLine = _businessLineService.GetAllByType(1).Data;
                    List<AuditScopesAndCriteria> scopesAndCriterias = new List<AuditScopesAndCriteria>();
                    List<Findings> Findings = new List<Findings>();


                    if (auditSchedule.ScopeIds != null)
                    {
                        foreach (var id in auditSchedule.ScopeIds)
                        {
                            scopesAndCriterias.Add(_auditScopesAndCriteriaService.GetByID(id, true).Data);
                        }
                    }

                    if (auditSchedule.CriteriaIds != null)
                    {
                        foreach (var id in auditSchedule.CriteriaIds)
                        {
                            scopesAndCriterias.Add(_auditScopesAndCriteriaService.GetByID(id, true).Data);

                        }
                    }

                    var finding = _findingsService.GetAllByScheduleID(scheduleId);

                    string FindingsListDescription = "";
                    string FindingsScopesListDescription = "";
                    string responsibleList = "";
                    string findingTypeString = "";
                    string scopeName = "";
                    string scopeShortName = "";
                    int scopeId = 0;
                    List<string> listEmployee = new List<string>();
                    List<FindingsDetailByPdf> findingsIdsArrays = new List<FindingsDetailByPdf>();
                    var index = 1;
                    List<AuditScopesAndCriteria> scopes = scopesAndCriterias
                        .Where(q => q.Type == (int)ScopesAndCriteriasEnum.Scope).ToList();
                    List<AuditScopesAndCriteria> criterias = scopesAndCriterias
                        .Where(q => q.Type == (int)ScopesAndCriteriasEnum.Criteria).ToList();
                    if (finding.IsSuccess)
                        foreach (var itemScope in scopes)
                        {
                            List<int> Major = new List<int>();
                            List<int> Minor = new List<int>();
                            List<int> Observation = new List<int>();
                            List<int> BestPractice = new List<int>();
                            FindingsDetailByPdf findingsIdsArray = new FindingsDetailByPdf();

                            foreach (var f in finding.Data.Where(q => q.ScopeID == itemScope.AuditScopesAndCriteriaID)
                                         .ToList())
                            {

                                if (f.ClassficationIDs != null)
                                {
                                    f.IdentificationDate = DateTime.Now.ToLocalTime();

                                    f.ResponsibleIDList = (Array.ConvertAll(f.ResponsibleIDs.Split(",").ToArray(),
                                        s => int.Parse(s)));


                                    if (f.ClassficationIDs == (int)ClassficationEnum.Major)
                                    {
                                        findingTypeString = "MJR";
                                        f.DueDate = DateTime.Now.AddDays(21);
                                        Major.Add(f.ClassficationIDs);
                                    }
                                    else if (f.ClassficationIDs == (int)ClassficationEnum.Minor)
                                    {
                                        findingTypeString = "MNR";
                                        f.DueDate = DateTime.Now.AddDays(14);
                                        Minor.Add(f.ClassficationIDs);
                                    }
                                    else if (f.ClassficationIDs == (int)ClassficationEnum.Observation)
                                    {
                                        findingTypeString = "OBS";
                                        f.DueDate = DateTime.Now.AddDays(1);
                                        Observation.Add(f.ClassficationIDs);
                                    }
                                    else if (f.ClassficationIDs == (int)ClassficationEnum.BestPractice)
                                    {
                                        findingTypeString = "BP";
                                        f.DueDate = DateTime.Now;
                                        BestPractice.Add(f.ClassficationIDs);
                                    }

                                    _findingsService.Update(f);


                                }

                                html = html.Replace("{ActDate}",
                                    auditSchedule.ActDate == null ? "" : auditSchedule.ActDate.ToString("dd-MM-yyyy"));
                                html = html.Replace("{ActEndDate}",
                                    auditSchedule.ActEndDate == null
                                        ? ""
                                        : auditSchedule.ActEndDate.ToString("dd-MM-yyyy"));
                                html = html.Replace("{DateTimeNow}", DateTime.Now.ToString("dd-MM-yyyy"));

                                html = html.Replace("{IdentificationDate}",
                                    auditSchedule.ActDate.ToString("dd-MM-yyyy"));
                                html = html.Replace("{DueDate}", auditSchedule.ActEndDate.ToString("dd-MM-yyyy"));

                                html = html.Replace("{Aud}", auditSchedule.AuditRefNumber);



                                if (f.ResponsibleIDList != null)
                                {
                                    foreach (var item in f.ResponsibleIDList)
                                    {
                                        var users = _uatService.UserDetailById(JWToken, item).Data;
                                        var empDetail = _employeeService.GetByUserID(users.ID).Data;
                                        //responsibleList += @$"<p style = ""color: black; font-family: Arial, sans-serif; font-style: normal; font-weight: normal; text-decoration: none; font-size: 11pt; padding-top: 3pt; padding-left: 5pt; text-indent: 0pt; text-align: left;"">{employeeName}</p>";
                                        listEmployee.Add(
                                            @$"<tr><td style = ""width: 30%;"" ><span style = ""color: #012169;"">{users.FullName}</span></td><td style = ""width: 30%; background-color: #ffffff;""><span style = ""color: #012169;"" >{(empDetail != null ? empDetail.JobTitle ?? "" : users.UserName)}</span></td><td style = ""width: 40%; background-color: #ffffff;""><span style = ""color: #012169;"">{users.Email}</span></td></tr>");
                                    }
                                }

                                var scopeAndCriteria = _auditScopesAndCriteriaService.GetByID(f.ScopeID).Data;
                                if (scopeAndCriteria != null)
                                {
                                    scopeName = scopeAndCriteria.Title;
                                    scopeShortName = scopeAndCriteria.ShortName;
                                    scopeId = scopeAndCriteria.AuditScopesAndCriteriaID;
                                }

                                FindingsListDescription +=
                                    @$"<tr><td style = ""width:10%;""> {index}</td><td style =""width:10%;""> {findingTypeString} </td><td style = ""width:10%;""> {scopeShortName} </td><td style = ""width: 70%;"">{f.Description}</td></tr> ";
                                if (!string.IsNullOrEmpty(f.UploadFiles))
                                {
                                    var images = f.UploadFiles.Split(",").Where(q => !q.Contains(".pdf")).ToList();
                                    if (images.Count > 0)
                                    {
                                        var img =
                                            @"<tr><td style =""width: 100%;"" colspan = ""4"" >{images}</td></tr>";
                                        var imgcontent = "";
                                        foreach (var item in images)
                                        {
                                            imgcontent += @$"
                                            <img style = ""width: 205px;"" src = ""{Globals.GetImageUrl(item)}"">
                                            ";
                                        }

                                        img = img.Replace("{images}", imgcontent);
                                        FindingsListDescription += img;
                                    }

                                }

                                index++;
                            }

                            findingsIdsArray.scopeMajor = Major.Count;
                            findingsIdsArray.scopeMinor = Minor.Count;
                            findingsIdsArray.scopeObs = Observation.Count;
                            findingsIdsArray.scopeBestPractice = BestPractice.Count;
                            findingsIdsArray.scopeId = itemScope.AuditScopesAndCriteriaID;
                            findingsIdsArray.scopeName = itemScope.Title;
                            findingsIdsArray.scopeShortName = itemScope.ShortName;
                            findingsIdsArrays.Add(findingsIdsArray);

                        }

                    foreach (var emp in listEmployee.Distinct())
                    {
                        responsibleList += emp;
                    }

                    var totalMajor = 0;
                    var totalMinor = 0;
                    var totalObservation = 0;
                    var totalBestPractice = 0;
                    foreach (var i in findingsIdsArrays)
                    {
                        string major = i.scopeMajor == 0 ? "" : i.scopeMajor.ToString();
                        string minor = i.scopeMinor == 0 ? "" : i.scopeMinor.ToString();
                        string observation = i.scopeObs == 0 ? "" : i.scopeObs.ToString();
                        string bestpractice = i.scopeBestPractice == 0 ? "" : i.scopeBestPractice.ToString();
                        totalMajor += i.scopeMajor;
                        totalMinor += i.scopeMinor;
                        totalObservation += i.scopeObs;
                        totalBestPractice += i.scopeBestPractice;
                        FindingsScopesListDescription +=
                            @$"<tr><td style = ""width: 50%; background-color: #ffffff;""><span style=""color: #012169;"">{i.scopeName}</span></td><td style = ""width: 12.5%; background-color: #ffffff; text-align: center;""><span style=""color: #012169;"">{bestpractice}</span></td><td style = ""width: 12.5%; background-color: #ffffff; text-align: center;""><span style=""color: #012169;"">{major}</span></td><td style = ""width: 12.5%; background-color: #ffffff; text-align: center;""><span style=""color: #012169;"">{minor}</span></td><td style = ""width: 12.5%; background-color: #ffffff; text-align: center;""><span style=""color: #012169;"">{observation}</span></td></tr>";
                    }



                    html = html.Replace("{Major}", totalMajor == 0 ? "" : totalMajor.ToString());
                    html = html.Replace("{Minor}", totalMinor == 0 ? "" : totalMinor.ToString());
                    html = html.Replace("{Observation}", totalObservation == 0 ? "" : totalObservation.ToString());
                    html = html.Replace("{BestPractice}", totalBestPractice == 0 ? "" : totalBestPractice.ToString());

                    html = html.Replace("{FindingsDetail}", FindingsListDescription);
                    html = html.Replace("{AuditMethodName}", auditMethod.Name);
                    html = html.Replace("{BusinessLineName}",
                        businessLine.FirstOrDefault(q => q.BussinesLineID == auditCard.BusinessLineID)?.Name);
                    html = html.Replace("{ResponsibleList}", responsibleList);
                    html = html.Replace("{AuditMethodology}", auditSchedule.AuditMethodology);
                    html = html.Replace("{AuditObjectives}", auditSchedule.AuditObjectives);
                    html = html.Replace("{SendMailDate}", DateTime.Now.ToString("dd-MM-yyyy"));
                    html = html.Replace("{AuditCardsTitle}", auditCard.Name);

                    if (auditSchedule.CriteriaIds != null)
                        scopesAndCriterias.AddRange(auditSchedule.CriteriaIds.Select(id =>
                            _auditScopesAndCriteriaService.GetByID(id).Data));


                    html = html.Replace("{FindingsScopesDetail}", FindingsScopesListDescription);

                    if (scopes.Any())
                    {
                        string scopeTitle = scopes.Aggregate("", (current, sc) => current + (sc.Title + "<br/>"));
                        html = html.Replace("{Scope.Title}", scopeTitle);

                    }

                    if (criterias.Any())
                    {
                        string criteriaTitle = criterias.Aggregate("", (current, cr) => current + (cr.Title + "<br/>"));
                        html = html.Replace("{Criterias.Title}", criteriaTitle);
                    }


                    html = html.Replace("{Auditors}", auditorList);
                    html = html.Replace("{LeadAuditor}", LeaduserList);
                    html = html.Replace("{AuditorsListDetail}", userList);
                    html = html.Replace("{ProjectCode}", auditCard.ProjectCode ?? "");

                    html = html.Replace("{ScheduleType.Name}", type.Name);
                    html = html.Replace("{ScheduleType.Parent}", Parent.Name);

                    html = html.Replace("{PDFCreateDate}", DateTime.Now.ToString(CultureInfo.InvariantCulture));

                    //Byte[] res = null;
                    //MemoryStream ms = new MemoryStream();
                    //var config = new PdfGenerateConfig();
                    //config.PageOrientation = PdfSharp.PageOrientation.Landscape;
                    //config.ManualPageSize = new PdfSharp.Drawing.XSize(1132, 842);
                    //config.SetMargins(10);

                    //var pdf = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(html.ToString(), config);
                    //pdf.Save(ms);
                    //res = ms.ToArray();
                    //pdf.Dispose();
                    //ms.Close();
                    SelectPdf.HtmlToPdf htmlToPdf = new SelectPdf.HtmlToPdf
                    {
                        Options =
                        {
                            PdfPageOrientation = PdfPageOrientation.Portrait,
                            MarginLeft = 15,
                            MarginRight = 15,
                            MarginTop = 15,
                            MarginBottom = 15,
                            EmbedFonts = true
                        }
                    };

                    SelectPdf.PdfDocument pdfDocument = htmlToPdf.ConvertHtmlString(html);
                    SelectPdf.PdfFont font = pdfDocument.AddFont(PdfStandardFont.Helvetica);
                    pdfDocument.Fonts.Add(font);
                    byte[] pdf = pdfDocument.Save();

                    pdfDocument.Close();
                    File.WriteAllBytes(@"wwwroot\Pdfs\" + auditSchedule.AuditRefNumber + ".pdf", pdf);
                    report.Content = html.ToString();
                    report.AuditScheduleID = auditSchedule.AuditScheduleID;
                    report.Path = @"wwwroot\Pdfs\" + auditSchedule.AuditScheduleID.ToString() + ".pdf";
                    result = _auditReportService.Insert(report);

                }

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }

        public static ServiceResult<Actions> SendActionInfo([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService, [FromServices] IActionsService _actionService,
            [FromServices] IFindingsService _findingsService,
            
            [FromServices] IUserService _userService,
            [FromServices] IAuditCardsService _auditCardsService,
            [FromServices] IAuditScheduleService _auditScheduleService,
            Actions action, string JWToken)


        {

            var result = new ServiceResult<Actions>();
            try
            {
                List<UatUserDetail> user = new List<UatUserDetail>();
                List<string> userMail = new List<string>();
                //string link = "https://audit.enka.com/FindingsList";
                var planedDate = "";
                if (action.ActionStartDate != DateTime.MinValue)
                    planedDate += action.ActionStartDate.ToString("dd-MM-yyyy");
                if (action.ActionDueDate != DateTime.MinValue)
                    planedDate += "-" + action.ActionDueDate.ToString("dd-MM-yyyy");
                string link = "https://audit.enka.com/action-detail?actionId=" + action.ActionsID;
                var finding = _findingsService.GetByID(action.FindingID).Data;

                var ownerFullName = string.Empty;
                if (!string.IsNullOrEmpty(finding.ResponsibleIDs))
                {
                    var responsibleIds = (Array.ConvertAll(finding.ResponsibleIDs.Split(",").ToArray(), s => int.Parse(s)));
                    try
                    {
 var aUserData = _userService.GetByID(responsibleIds[0]).Data;
                    ownerFullName = $"{aUserData.FirstName} {aUserData.LastName}";
                    }
                    catch (Exception e)
                    {
                    }
                   
                }
                
                var schedule = _auditScheduleService.GetByID(finding.ScheduleID);
                var auditcard = _auditCardsService.GetByID(schedule.Data.AuditCardID).Data?.AreaToAudit;
                
                //178
                #region DENETÇİYE KANIT DOKÜMANI YÜKLENDİ BİLGİLENDİRMESİ
                string mailBody = "<p>Sayın Yetkili,<br/> Aşağıda bilgileri verilen bulguya, sorumlu kişi " + ownerFullName+
                                    " tarafından kanıt dokümanı yüklenmiştir. Bulgunun kapatılması için onayınız beklenmektedir.<br/><br/>" +
                                     "<strong>Proje Adı:</strong> " + auditcard + "<br/>" +
                                      "<strong>Denetim Tarihi:</strong> " + planedDate + "<br/>" +
                                      "<strong>Bulgu No:</strong> " + finding.FindingsNo + "<br/>"
                                      + "<strong> Bulgu Başlığı: </strong>" + finding.Title + "<br/>"
                                      + "<strong> Eylem Planı No: </strong>" + action.ActionNo + "<br/>" 
                                      +"<strong> Bulgu Durumu: </strong>" + finding.StatusID + "<br/><br/>"
                                      + "Sisteme giriş yapmak ve kanıt dokümanını görüntülemek için lütfen aşağıdaki bağlantıya tıklayınız:<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Saygılarımızla,<br/>ENKA AMS Yönetimi</p></strong>";

                mailBody +=
                    "<p>Bu bilgilendirme e-postası Audit Management System tarafından otomatik olarak iletilmiştir.</br>(For English, please see below.)</p>";

                mailBody +=
                    "<p>--------------------------------------------------------------------------------------------------------------------------------------------</p>";

                mailBody += "<p>Dear Responsible Person" + ",<br/> An evidence document has been added to the action plan, information of which is given below, by the responsible person " + ownerFullName +
                    ". Your approval is awaited in order to close the related finding.<br/><br/>" +
                                        "<strong>Project Name:</strong> " + auditcard + "<br/>" +
                                        "<strong>Audit Date:</strong> " + planedDate + "<br/>" +
                                        "<strong>Finding No:</strong> " + finding.FindingsNo + "<br/>"
                                        + "<strong> Finding Title: </strong>" + finding.Title + "<br/>"
                                        + "<strong> Action Plan No: </strong>" + action.ActionNo + "<br/>"
                                        +"<strong> Finding Status: </strong>" + finding.StatusID + "<br/><br/>"
                                        + "Please click the link below to log in to the system and view the evidence document:<br/>" +
                                        $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Regards,<br/>ENKA AMS Management</p></strong>";

                mailBody +=
                    "<p>This notification e-mail has been sent automatically by the Audit Management System.</p>";
                #endregion

                string[] userTo = finding.OwnerIDs.Split(",");
                foreach (var to in userTo)
                {
                    var ownerEmail = _uatService.UserList(JWToken).Data.data
                        ?.FirstOrDefault(q => q.UserID == int.Parse(to)).Email;
                    userMail.Add(ownerEmail);
                }

                if (user.Count > 0)
                {
                    foreach (var item in user)
                    {
                        userMail.Add(item.Email);
                    }
                }

                if (userMail.Count > 0)
                {
                    _mailService.SendEmail(string.Join(",", userMail),
                    " Denetimi - " + action.ActionNo + " Kanıt Dokümanı Bilgilendirme & Onay / Evidence Document Approval", mailBody);

                }


                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }

        public static ServiceResult<Actions> SendActionReject([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService, [FromServices] IActionsService _actionService,
            [FromServices] IFindingsService _findingsService, Actions action, string JWToken, string ownerFullName)

        {
            var result = new ServiceResult<Actions>();
            try
            {

                List<UatUserDetail> user = new List<UatUserDetail>();
                List<string> userMail = new List<string>();
                var planedDate = "";
                if (action.ActionStartDate != DateTime.MinValue)
                    planedDate += action.ActionStartDate.ToString("dd-MM-yyyy");
                if (action.ActionDueDate != DateTime.MinValue)
                    planedDate += "-" + action.ActionDueDate.ToString("dd-MM-yyyy");
                string link = "https://audit.enka.com/FindingsList";
                //string link = "https://audit.enka.com/action-detail?actionId=" + action.ActionsID;
                var finding = _findingsService.GetByID(action.FindingID).Data;
                //string mailBody = "<p>Sayın Yetkili,<br/>" + finding?.FindingsNo + " numaralı " + finding.Title +
                //                  " isimli bulguya ait olan" + action.ActionNo + " numaralı eylem" + @ownerFullName +
                //                  " tarafından" + action.RejectRemark +
                //                  " nedeni ile reddedilmiştir. Lütfen eyleminizi düzenleyerek tekrar onaya sununuz.</p><label>LINK :" +
                //                  $" <a href=\"{link}\">GİRİŞ</a></label>";

                //mailBody += "<strong><p>Saygılarımızla,</ p><p> ENKA İç Denetim Ekibi</p></strong>";

                #region DENETÇİDEN ÇALIŞANA EYLEM PLANI REDDEDİLDİ BİLGİLENDİRMESİ

                 string mailBody = "<p>Sayın Çalışan,<br/> Aşağıda bilgisi verilen denetim bulgusuna {actionbyEmployeeDateTime} tarihinde eklediğiniz eylem planı, "
                    + @ownerFullName + " tarafından gözden geçirilmiş ve reddedilmiştir.<br/>"

                   + "Bulgu ve eylem planıyla ilgili Denetçi tarafından yorum verilmiş olması halinde, sistemde görüntüleyebilirsiniz. Revize edilmiş eylem planını" + finding.DueDate + " tarihine kadar sisteme eklemeniz gerekmektedir. <br/><br/>" + "<strong>Proje Adı:</strong> " + "auditcard " + "<br/>" + "<strong>Denetim Tarihi:</strong> " + planedDate + "<br/>" + "<strong>Bulgu No:</strong> " + finding.FindingsNo + "<br/>" + "<strong> Bulgu Başlığı: </strong>" + finding.Title + "<br/>" + "<strong> Eylem Planı No: </strong>" + action.ActionNo + "<br/>" + "<strong> Bulgu Durumu: </strong>" + finding.StatusID + "<br/><br/>"
                      + " Sisteme giriş yapmak ve eylem planı revize etmek için lütfen aşağıdaki bağlantıya tıklayınız:<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Saygılarımızla,<br/>ENKA İç Denetim Ekibi</p></strong>";

                mailBody +=
                    "<p>Bu bilgilendirme e-postası Audit Management System tarafından otomatik olarak iletilmiştir.</br>(For English, please see below.)</p>";

                mailBody +=
                    "<p>--------------------------------------------------------------------------------------------------------------------------------------------</p>";

                mailBody += "<p>Dear Employee" + ",<br/> The action plan you added to the audit finding below on {actionbyEmployeeDateTime} has been reviewed and rejected by " + @ownerFullName + ". <br/>If any comments are given by the Auditor regarding the finding and action plan, you can view them in the system. You need to add the revised action plan to the system until " + @finding.DueDate + ".<br/><br/>" +
                     "<strong>Project Name:</strong> " + "auditcard" + "<br/>" +
                     "<strong>Audit Date:</strong> " + planedDate + "<br/>" +
                      "<strong>Finding No:</strong> " + finding.FindingsNo + "<br/>"
                      + "<strong> Finding Title: </strong>" + finding.Title + "<br/>"
                      + "<strong> Action Plan No: </strong>" + action.ActionNo + "<br/> "
                      + "<strong> Finding Status: </strong>" + finding.StatusID + "<br/><br/>"
                     + "Please click on the link below to log in and revise your action plan:<br/>"
                 + $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Regards,<br/>ENKA Internal Audit Team</p></strong>";

                mailBody +=
                    "<p>This notification e-mail has been sent automatically by the Audit Management System.</p>";
                #endregion


                string[] userTo = finding.ResponsibleIDs.Split(",");
                foreach (var to in userTo)
                {
                    var ownerEmail = _uatService.UserList(JWToken).Data.data
                        ?.FirstOrDefault(q => q.UserID == int.Parse(to)).Email;
                    userMail.Add(ownerEmail);
                }

                if (user.Count > 0)
                {
                    foreach (var item in user)
                    {
                        userMail.Add(item.Email);
                    }
                }

                if (userMail.Count > 0)
                {
                    _mailService.SendEmail(string.Join(",", userMail),
                   " Denetimi - " + action.ActionNo + " Numaralı Eylem Planı Reddi / Action Plan Rejection", mailBody);
                }


                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }

        public static ServiceResult<Actions> SendActionClosed([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService, [FromServices] IActionsService _actionService,
            [FromServices] IFindingsService _findingsService, IAuditorsTypeMappingService _auditorsTypeMappingService,
            Actions action, string JWToken, string ownerFullName, string auditcard)

        {
            var result = new ServiceResult<Actions>();
            try
            {
                //onaylanan kanıt mail ile gönderilecek!

                List<UatUserDetail> user = new List<UatUserDetail>();
                List<UatUserDetail> Leaduser = new List<UatUserDetail>();
                List<string> userMail = new List<string>();
                var planedDate = "";
                if (action.ActionStartDate != DateTime.MinValue)
                    planedDate += action.ActionStartDate.ToString("dd-MM-yyyy");
                if (action.ActionDueDate != DateTime.MinValue)
                    planedDate += "-" + action.ActionDueDate.ToString("dd-MM-yyyy");
                string link = "https://audit.enka.com/FindingsList";
                //string link = "https://audit.enka.com/action-detail?actionId=" + action.ActionsID;
                var finding = _findingsService.GetByID(action.FindingID).Data;
                var auditorsTypeMapping = _auditorsTypeMappingService.GetByTemplateID(finding.ScheduleID).Data;
                if (auditorsTypeMapping.Count > 0)
                {
                    foreach (var item in auditorsTypeMapping)
                    {
                        item.UserID = Array.ConvertAll(item.UserIDs.Split(",").ToArray(), s => int.Parse(s));
                        foreach (var itemUserIDs in item.UserID)
                        {
                            var users = _uatService.UserDetailById(JWToken, itemUserIDs).Data;
                            user.Add(users);
                            if (item.AuditorsTypeID == (int)AuditorEnum.LeadAuditor)
                            {
                                Leaduser.Add(users);
                            }
                        }
                    }
                }

                #region ÇALIŞANA BULGU KAPANDI TEŞEKKÜRLER BİLGİLENDİRMESİ

                string mailBody = "<p>Sayın Yetkili,<br/> Aşağıda bilgisi verilen denetim bulgusuna {secondactionplanSendDate} tarihinde yüklediğiniz kanıt dokümanı, ENKA Denetim Ekibi tarafından onaylanmış ve bulgu kapatılmıştır. <br/><br/>Denetim süresince göstermiş olduğunuz iş birliği için teşekkür ederiz. Bulgunun kapatılması için onayınız beklenmektedir.<br/><br/>" +
                                     "<strong>Proje Adı:</strong> " + auditcard + "<br/>" +
                                      "<strong>Denetim Tarihi:</strong> " + planedDate + "<br/>" +
                                      "<strong>Bulgu No:</strong> " + finding.FindingsNo + "<br/>"
                                      + "<strong> Bulgu Başlığı: </strong>" + finding.Title + "<br/>"
                                      + "<strong> Eylem Planı No: </strong>" + action.ActionNo + "<br/>"
                                      + "<strong> Bulgu Durumu: </strong>" + finding.StatusID + "<br/><br/>"
                                      + "Sisteme giriş yapmak için lütfen aşağıdaki bağlantıya tıklayınız:<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Saygılarımızla,<br/>ENKA Denetim Ekibi</p></strong>";

                mailBody +=
                    "<p>Bu bilgilendirme e-postası Audit Management System tarafından otomatik olarak iletilmiştir.</br>(For English, please see below.)</p>";

                mailBody +=
                    "<p><hr></p>";

                mailBody += "<p>Dear Responsible Person" + ",<br/> The evidence document you uploaded to the action plan, information of which is given below, on {secondactionplanSendDate} was approved by the ENKA Audit team and the finding was closed.<br/><br/>" +

                    "We would like to thank you for your cooperation during the audit.<br/><br/> " + 
                    "<strong>Project Name:</strong> " + auditcard + "<br/>" +
                     "<strong>Audit Date:</strong> " + planedDate + "<br/>" +
                      "<strong>Finding No:</strong> " + finding.FindingsNo + "<br/>"
                      + "<strong> Finding Title: </strong>" + finding.Title + "<br/>"
                      + "<strong> Action Plan No: </strong>" + action.ActionNo + "<br/>"
                      + "<strong> Finding Status: </strong>" + finding.StatusID + "<br/><br/>"
                     + "Please click the link below to log in to the system :<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Regards,<br/>ENKA Audit Team</p></strong>";

                mailBody +=
                    "<p>This notification e-mail has been sent automatically by the Audit Management System.</p>";
                #endregion

                #region LEAD AUDITORE BULGU KAPANDI BİLGİLENDİRMESİ

                mailBody = "<p>Sayın Baş Denetiçimiz,<br/> Aşağıda bilgisi verilen denetim bulgusuna, {secondactionplanSendDate} tarihinde sorumlu kişi " + @ownerFullName + " tarafından yüklenen kanıt dokümanı, {bulgukapatmaDate} tarihinde denetçi " + "AuditorName" + " tarafından onaylanmıştır ve bulgu kapatılmıştır.<br/><br/>" +
                                     "<strong>Proje Adı:</strong> " + auditcard + "<br/>" +
                                      "<strong>Denetim Tarihi:</strong> " + planedDate + "<br/>" +
                                      "<strong>Bulgu No:</strong> " + finding.FindingsNo + "<br/>"
                                      + "<strong> Bulgu Başlığı: </strong>" + finding.Title + "<br/>"
                                      + "<strong> Eylem Planı No: </strong>" + action.ActionNo + "<br/>"
                                      + "<strong> Bulgu Durumu: </strong>" + finding.StatusID + "<br/><br/>"
                                      + "Sisteme giriş yapmak için lütfen aşağıdaki bağlantıya tıklayınız:<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Saygılarımızla,<br/>ENKA AMS Yönetimi</p></strong>";

                mailBody +=
                    "<p>Bu bilgilendirme e-postası Audit Management System tarafından otomatik olarak iletilmiştir.</br>(For English, please see below.)</p>";

                mailBody +=
                    "<p>--------------------------------------------------------------------------------------------------------------------------------------------</p>";

                mailBody += "<p>Dear LeadAuditor" + ",<br/> The evidence document uploaded by the responsible person " + @ownerFullName + " to the action plan on {secondactionplanSendDate}, the information of which is given below, was approved by the auditor " + "AuditorName" + " on " + "{bulgukapatmaDate}" + 
                                      
                  " and the finding was closed.<br/><br/>" +


                    "<strong>Project Name:</strong> " + auditcard + "<br/>" +
                     "<strong>Audit Date:</strong> " + planedDate + "<br/>" +
                      "<strong>Finding No:</strong> " + finding.FindingsNo + "<br/>"
                      + "<strong> Finding Title: </strong>" + finding.Title + "<br/>"
                      + "<strong> Action Plan No: </strong>" + action.ActionNo + "<br/>"
                      + "<strong> Finding Status: </strong>" + finding.StatusID + "<br/><br/>"
                     + "Please click the link below to log in to the system :<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Regards,<br/>ENKA AMS Management</p></strong>";

                mailBody +=
                    "<p>This notification e-mail has been sent automatically by the Audit Management System.</p>";
                #endregion

                SelectPdf.HtmlToPdf htmlToPdf = new SelectPdf.HtmlToPdf();
                htmlToPdf.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                htmlToPdf.Options.MarginLeft = 15;
                htmlToPdf.Options.MarginRight = 15;
                htmlToPdf.Options.MarginTop = 15;
                htmlToPdf.Options.MarginBottom = 15;
                htmlToPdf.Options.EmbedFonts = true;

                string[] userTo = finding.ResponsibleIDs.Split(",");
                foreach (var to in userTo)
                {
                    var owner = _uatService.UserList(JWToken).Data.data?.FirstOrDefault(q => q.UserID == int.Parse(to));
                    if (owner != null)
                    {
                        var ownerEmail = owner.Email;
                        userMail.Add(ownerEmail);
                    }

                }

                if (Leaduser.Count > 0)
                {
                    foreach (var item in Leaduser)
                    {
                        if (!userMail.Contains(item.Email))
                        {
                            userMail.Add(item.Email);

                        }
                    }
                }

                if (userMail.Count > 0)
                {
                    _mailService.SendEmail(string.Join(",", userMail),
                        auditcard + " Denetimi - " + action.ActionNo + " Numaralı Eylem Planı Onayı / Action Plan Approval", mailBody);
                }


                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }

        public static ServiceResult<Actions> SendActionAccept([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService, [FromServices] IActionsService _actionService,
            [FromServices] IFindingsService _findingsService, Actions action, string JWToken, string ownerFullName, 
            string auditcard)

        {
            var result = new ServiceResult<Actions>();
            try
            {
                //onaylanan kanıt mail ile gönderilecek!

                List<UatUserDetail> user = new List<UatUserDetail>();
                List<string> userMail = new List<string>();
                var planedDate = "";
                if (action.ActionStartDate != DateTime.MinValue)
                    planedDate += action.ActionStartDate.ToString("dd-MM-yyyy");
                if (action.ActionDueDate != DateTime.MinValue)
                    planedDate += "-" + action.ActionDueDate.ToString("dd-MM-yyyy");
                //string link = "https://audit.enka.com/ActionsList";
                string link = "https://audit.enka.com/action-detail?actionId=" + action.ActionsID;
                var finding = _findingsService.GetByID(action.FindingID).Data;

                #region DENETÇİDEN ÇALIŞANA EYLEM PLANI ONAYLANDI BİLGİLENDİRMESİ
                
                string mailBody = "<p>Sayın Çalışan,<br/> Aşağıda bilgisi verilen denetim bulgusuna {actionbyEmployeeDateTime} tarihinde eklediğiniz eylem planı, " 
                    + @ownerFullName + " tarafından onaylanmıştır. Bulgunun kapatılması için " + finding.DueDate + " tarihine kadar bir kanıt dokümanı yüklemeniz beklenmektedir.<br/><br/>" + "<strong>Proje Adı:</strong> " + auditcard + "<br/>" + "<strong>Denetim Tarihi:</strong> " + planedDate + "<br/>"  + "<strong>Bulgu No:</strong> " + finding.FindingsNo + "<br/>" + "<strong> Bulgu Başlığı: </strong>" + finding.Title + "<br/>" + "<strong> Eylem Planı No: </strong>" + action.ActionNo + "<br/>" + "<strong> Bulgu Durumu: </strong>" + finding.StatusID + "<br/><br/>" 
                      + " Sisteme giriş yapmak ve kanıt dokümanı yüklemek için lütfen aşağıdaki bağlantıya tıklayınız:<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Saygılarımızla,<br/>ENKA İç Denetim Ekibi</p></strong>";

                mailBody +=
                    "<p>Bu bilgilendirme e-postası Audit Management System tarafından otomatik olarak iletilmiştir.</br>(For English, please see below.)</p>";

                mailBody +=
                    "<p>--------------------------------------------------------------------------------------------------------------------------------------------</p>";

                mailBody += "<p>Dear Employee" + ",<br/> The action plan you added to the audit finding below on {actionbyEmployeeDateTime} has been approved by " + @ownerFullName + ". You are expected to upload an evidence document by " + @finding.DueDate + " to close the finding.<br/><br/>" +
                     "<strong>Project Name:</strong> " + auditcard + "<br/>" +
                     "<strong>Audit Date:</strong> " + planedDate + "<br/>" +
                      "<strong>Finding No:</strong> " + finding.FindingsNo + "<br/>"
                      + "<strong> Finding Title: </strong>" + finding.Title + "<br/>"
                      + "<strong> Action Plan No: </strong>" + action.ActionNo + "<br/> "
                      +"<strong> Finding Status: </strong>" + finding.StatusID + "<br/><br/>"
                     + "Please click on the link below to log in and upload your evidence document:<br/>" 
                 +$" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Regards,<br/>ENKA Internal Audit Team</p></strong>";

                mailBody +=
                    "<p>This notification e-mail has been sent automatically by the Audit Management System.</p>";
                #endregion


                SelectPdf.HtmlToPdf htmlToPdf = new SelectPdf.HtmlToPdf();
                htmlToPdf.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                htmlToPdf.Options.MarginLeft = 15;
                htmlToPdf.Options.MarginRight = 15;
                htmlToPdf.Options.MarginTop = 15;
                htmlToPdf.Options.MarginBottom = 15;
                htmlToPdf.Options.EmbedFonts = true;


                string[] userTo = finding.ResponsibleIDs.Split(",");
                foreach (var to in userTo)
                {
                    var ownerEmail = _uatService.UserList(JWToken).Data.data
                        ?.FirstOrDefault(q => q.UserID == int.Parse(to)).Email;
                    userMail.Add(ownerEmail);
                }

                if (user.Count > 0)
                {
                    foreach (var item in user)
                    {
                        userMail.Add(item.Email);
                    }
                }

                if (userMail.Count > 0)
                {
                    _mailService.SendEmail(string.Join(",", userMail),
                        auditcard + " Denetimi - " + action.ActionNo + " Numaralı Eylem Planı Bilgilendirme & Onay / Action Plan Approval", mailBody);
                }


                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }

        public static ServiceResult<Actions> SendNewActionInfo([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService, [FromServices] IActionsService _actionService,
            [FromServices] IFindingsService _findingsService, Actions action, string JWToken, string ownerFullName,
            string auditcard)

        {
            var result = new ServiceResult<Actions>();
            try
            {
                //Employeenin yükledigi eylem planından sonra Denetciye gelen email icerigi!
 
                List<UatUserDetail> user = new List<UatUserDetail>();
                List<string> userMail = new List<string>();
                var planedDate = "";
                if (action.ActionStartDate != DateTime.MinValue)
                    planedDate += action.ActionStartDate.ToString("dd-MM-yyyy");
                if (action.ActionDueDate != DateTime.MinValue)
                    planedDate += "-" + action.ActionDueDate.ToString("dd-MM-yyyy");
               // string link = "https://audit.enka.com/ActionsList";
               string link = "https://audit.enka.com/action-detail?actionId=" + action.ActionsID;
                var finding = _findingsService.GetByID(action.FindingID).Data;

                #region ESKİ EMAİL BODYLERİ BURDA


                #endregion
                #region DENETÇİYE EYLEM PLANI YÜKLENDİ BİLGİLENDİRMESİ
                string mailBody = "<p>Sayın Yetkili,<br/> Aşağıda bilgileri verilen bulguya, sorumlu kişi " + @ownerFullName +
                                    " tarafından yeni bir eylem planı eklenmiştir. Onayınız beklenmektedir.<br/><br/>" +
                                     "<strong>Proje Adı:</strong> " + auditcard + "<br/>" +
                                      "<strong>Denetim Tarihi:</strong> " + planedDate + "<br/>" +
                                      "<strong>Bulgu No:</strong> " + finding.FindingsNo + "<br/>"
                                      + "<strong> Bulgu Başlığı: </strong>" + finding.Title +"<br/>" 
                                      + "<strong> Eylem Planı No: </strong>" + action.ActionNo + "<br/><br/> Sisteme giriş yapmak ve eylemi görüntülemek, onaylamak/reddetmek için lütfen aşağıdaki bağlantıya tıklayınız:<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Saygılarımızla,<br/>ENKA AMS Yönetimi</p></strong>";

                mailBody +=
                    "<p>Bu bilgilendirme e-postası Audit Management System tarafından otomatik olarak iletilmiştir.</br>(For English, please see below.)</p>";

                mailBody +=
                    "<p>--------------------------------------------------------------------------------------------------------------------------------------------</p>";

                mailBody += "<p>Dear Responsible Person"  + ",<br/> A new action plan has been added to the finding, information of which is given below, by the responsible person " + @ownerFullName +
                    ". Your approval is awaited.<br/><br/>" +
                     "<strong>Project Name:</strong> " + auditcard + "<br/>" +
                     "<strong>Audit Date:</strong> " + planedDate + "<br/>" +
                      "<strong>Finding No:</strong> " + finding.FindingsNo + "<br/>"
                      + "<strong> Finding Title: </strong>" + finding.Title + "<br/>" 
                      +"<strong> Action Plan No: </strong>" + action.ActionNo + "<br/><br/> Please click the link below to log in to the system and view, approve/reject the action:<br/>" +
                  $" <a href=\"{link}\">https://audit.enka.com</a></p>";

                mailBody += "<strong><p>Regards,<br/>ENKA AMS Management</p></strong>";

                mailBody +=
                    "<p>This notification e-mail has been sent automatically by the Audit Management System.</p>";
                #endregion

                SelectPdf.HtmlToPdf htmlToPdf = new SelectPdf.HtmlToPdf();
                htmlToPdf.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                htmlToPdf.Options.MarginLeft = 15;
                htmlToPdf.Options.MarginRight = 15;
                htmlToPdf.Options.MarginTop = 15;
                htmlToPdf.Options.MarginBottom = 15;
                htmlToPdf.Options.EmbedFonts = true;


                string[] userTo = finding.OwnerIDs.Split(",");
                foreach (var to in userTo)
                {
                    var ownerEmail = _uatService.UserList(JWToken).Data.data
                        ?.FirstOrDefault(q => q.UserID == int.Parse(to))?.Email;
                    userMail.Add(ownerEmail);
                }

                if (user.Count > 0)
                {
                    foreach (var item in user)
                    {
                        userMail.Add(item.Email);
                    }
                }

                if (userMail.Count > 0)
                {
                    _mailService.SendEmail(string.Join(",", userMail),
                        auditcard + " Denetimi - " + action.ActionNo + " Numaralı Eylem Planı Bilgilendirme & Onay / Action Plan Approval", mailBody);
                }


                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }

        public static ServiceResult<Findings> SendNewFindingInfo([FromServices] IMailService _mailService,
            [FromServices] IUatService _uatService, [FromServices] IActionsService _actionService,
            [FromServices] IFindingsService _findingsService, Findings finding, string JWToken, string ownerFullName)
        {
            var result = new ServiceResult<Findings>();
            try
            {
                //onaylanan kanıt mail ile gönderilecek!

                List<UatUserDetail> user = new List<UatUserDetail>();
                List<string> userMail = new List<string>();
                var planedDate = "";
                if (finding.DueDate != DateTime.MinValue)
                    planedDate += finding.DueDate.ToString("dd-MM-yyyy");
                if (finding.IdentificationDate != DateTime.MinValue)
                    planedDate += "-" + finding.IdentificationDate.ToString("dd-MM-yyyy") +
                                  " tarihleri arasında 'Action Plan' eklemelisiniz.";
                string link = "https://audit.enka.com/FindingsList";
                //string link = "https://audit.enka.com/finding-detail?findingId=" + finding.FindingsID;
                string mailBody = "<p>Sayın Yetkili,<br/>" + finding?.FindingsNo + " numaralı " + finding.Title +
                                  " isimli yeni bir bulgu " + @ownerFullName + " tarafından eklenmiştir." + planedDate +
                                  "</p><label>Sisteme girmek için :" +
                                  $" <a href=\"{link}\">https://audit.enka.com</a></label>";

                mailBody += "<strong><p>Saygılarımızla,</ p><p> ENKA İç Denetim Ekibi</p></strong>";
                SelectPdf.HtmlToPdf htmlToPdf = new SelectPdf.HtmlToPdf();
                htmlToPdf.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                htmlToPdf.Options.MarginLeft = 15;
                htmlToPdf.Options.MarginRight = 15;
                htmlToPdf.Options.MarginTop = 15;
                htmlToPdf.Options.MarginBottom = 15;
                htmlToPdf.Options.EmbedFonts = true;


                string[] userTo = finding.ResponsibleIDs.Split(",");
                foreach (var to in userTo)
                {
                    var employeeEmail = _uatService.UserList(JWToken).Data.data
                        ?.FirstOrDefault(q => q.UserID == int.Parse(to)).Email;
                    userMail.Add(employeeEmail);
                }

                if (user.Count > 0)
                {
                    foreach (var item in user)
                    {
                        userMail.Add(item.Email);
                    }
                }

                if (userMail.Count > 0)
                {
                    _mailService.SendEmail(string.Join(",", userMail),
                        finding.FindingsNo + " Numaralı Bulgu Tarafınıza atanmıştır.", mailBody);
                }


                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.IsSuccess = false;
            }


            return result;
        }

       public static ServiceResult<AuditReport> SendReportData(
    [FromServices] IMailService mailService,
    [FromServices] IUatService uatService,
    [FromServices] IAuditCardsService auditCardsService,
    [FromServices] IAuditorsTypeMappingService auditorsTypeMappingService,
    [FromServices] IAuditScheduleService auditScheduleService,
    [FromServices] IScheduleTypeService scheduleTypeService,
    AuditReport report,
    IWebHostEnvironment env,
    string JWToken)
{
    var result = new ServiceResult<AuditReport>();
    try
    {
        IDictionary<string, string> mailContents = new Dictionary<string, string>();
        string html = report.Content;

        // Auditors, LeadAuditors ve diğer kullanıcıların toplu olarak alınması
        List<UatUserDetail> auditors = new List<UatUserDetail>();
        List<UatUserDetail> leadAuditors = new List<UatUserDetail>();
        var auditSchedule = _auditScheduleService.GetByID(report.AuditScheduleID).Data;
        ScheduleType scheduleType = _scheduleTypeService.GetByID(auditSchedule.ScheduleTypeID).Data;
        var auditorsTypeMapping = _auditorsTypeMappingService.GetByTemplateID(auditSchedule.AuditScheduleID).Data;

        foreach (var mapping in auditorsTypeMapping)
        {
            mapping.UserID = Array.ConvertAll(mapping.UserIDs.Split(",").ToArray(), s => int.Parse(s));
            foreach (var userID in mapping.UserID)
            {
                var user = _uatService.UserDetailById(JWToken, userID).Data;
                auditors.Add(user);
                if (mapping.AuditorsTypeID == (int)AuditorEnum.LeadAuditor)
                {
                    leadAuditors.Add(user);
                }
            }
        }

        List<string> toEmails = auditSchedule.ToAudit.Split(",").ToList();
        if (!string.IsNullOrEmpty(report.ToAudit))
        {
            toEmails.Add(report.ToAudit);
        }

        List<string> ccEmails = auditSchedule.CcAudit.Split(",").ToList();
        if (!string.IsNullOrEmpty(report.CCAudit))
        {
            ccEmails.Add(report.CCAudit);
        }

        SelectPdf.HtmlToPdf htmlToPdf = new SelectPdf.HtmlToPdf();
        htmlToPdf.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
        htmlToPdf.Options.MarginLeft = 15;
        htmlToPdf.Options.MarginRight = 15;
        htmlToPdf.Options.MarginBottom = 15;
        htmlToPdf.Options.MarginTop = 15;
        htmlToPdf.Options.EmbedFonts = true;

        SelectPdf.PdfDocument pdfDocument = htmlToPdf.ConvertHtmlString(html);
        SelectPdf.PdfFont font = pdfDocument.AddFont(PdfStandardFont.Helvetica);
        pdfDocument.Fonts.Add(font);
        byte[] pdf = pdfDocument.Save();
        pdfDocument.Close();

        var auditCard = _auditCardsService.GetByID(auditSchedule.AuditCardID);
        string fileName = $"{auditCard.Data.ProjectCode}-{auditCard.Data.AreaToAudit}-Denetim Raporu-{scheduleType.Name}-{DateTime.Now.ToString("yyyy")}.pdf";

        File.WriteAllBytes(Path.Combine(env.WebRootPath, "Pdfs", fileName), pdf);

        List<string> recipientEmails = auditors
            .Where(u => !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => u.Email)
            .ToList();

        recipientEmails.AddRange(toEmails);

        if (recipientEmails.Count > 0)
        {
            _mailService.SendEmail(
                string.Join(",", recipientEmails),
                report.Title,
                report.Body,
                Path.Combine("wwwroot", "Pdfs", fileName),
                string.Join(",", ccEmails)
            );
        }

        result.IsSuccess = true;
    }
    catch (Exception ex)
    {
        result.Messages = ex.Message;
        result.IsSuccess = false;
    }

    return result;
}


        public static ServiceResult<AuditReport> SendTestReportData([FromServices] IMailService _mailService,
            [FromServices] IAuditCardsService _auditCardsService,
            [FromServices] IAuditScheduleService _auditScheduleService, [FromServices] IScheduleTypeService
                _scheduleTypeService, IWebHostEnvironment _env, AuditReport report)

        {
            var result = new ServiceResult<AuditReport>();
            try
            {
                IDictionary<string, string> mailContents = new Dictionary<string, string>();
                string html = report.Content;
                AuditSchedule auditSchedule = _auditScheduleService.GetByID(report.AuditScheduleID).Data;
                ScheduleType type = _scheduleTypeService.GetByID(auditSchedule.ScheduleTypeID).Data;

            

                SelectPdf.HtmlToPdf htmlToPdf = new SelectPdf.HtmlToPdf();
                htmlToPdf.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                htmlToPdf.Options.MarginLeft = 15;
                htmlToPdf.Options.MarginRight = 15;
                htmlToPdf.Options.MarginBottom = 15;
                htmlToPdf.Options.MarginTop = 15;
                htmlToPdf.Options.EmbedFonts = true;

                SelectPdf.PdfDocument pdfDocument = htmlToPdf.ConvertHtmlString(html);
                SelectPdf.PdfFont font = pdfDocument.AddFont(PdfStandardFont.Helvetica);
                pdfDocument.Fonts.Add(font);
                byte[] pdf = pdfDocument.Save();

                pdfDocument.Close();

                var auditCard = _auditCardsService.GetByID(auditSchedule.AuditCardID);
                
                var allFiles = Directory.GetFiles(_env.WebRootPath + "/Pdfs/", "*.*", SearchOption.AllDirectories);
                string fileName = "";
                if (allFiles.Contains(_env.WebRootPath + "/Pdfs/" + auditSchedule.AuditRefNumber + "-" + @type.Name + " Denetim Raporu" + ".pdf"))
                {
                    //fileName = auditSchedule.AuditRefNumber + "-" + @type.Name + " Denetim Raporu" + "-" + DateTime.Now.ToString("HH:mm") + ".pdf";
                    // fileName = auditSchedule.AuditCardID.ToString() + "-" + " Denetim Raporu" + "-" + @type.Name + "-" + DateTime.Now.ToString("yyyy") + ".pdf";
                    fileName = auditCard.Data.ProjectCode?.ToString() + "-" + auditCard.Data.AreaToAudit?.ToString() + "-" + " Denetim Raporu" + "-" + @type.Name + "-" + DateTime.Now.ToString("yyyy") + ".pdf";
                }
                else
                {
                    // fileName = auditSchedule.AuditRefNumber + "-" + @type.Name + " Denetim Raporu" + ".pdf";
                    fileName = auditCard.Data.ProjectCode?.ToString() + "-" + auditCard.Data.AreaToAudit?.ToString() + "-" + @type.Name + " Denetim Raporu" + ".pdf";
                }

                File.WriteAllBytes(_env.WebRootPath + "/Pdfs/" + fileName, pdf);

                _mailService.SendEmail(report.TestUserMail, report.Title, report.Body, @"wwwroot\Pdfs\" + fileName,
                    String.Empty);

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Messages = "There is a problem. Please try again!";
                result.IsSuccess = false;
            }


            return result;
        }

        private static PdfSharp.Pdf.PdfDocument ImportPdfDocument(PdfSharp.Pdf.PdfDocument pdf1)
        {
            using (var stream = new MemoryStream())
            {
                pdf1.Save(stream, false);
                stream.Position = 0;
                var result = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                return result;
            }
        }

        
    }
}
