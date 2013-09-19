using lecoati.uMobile;
using lecoati.uMobile.umComponents;
using lecoati.uMobile.umCore;
using lecoati.uMobile.umHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Forms.Core;
using Umbraco.Forms.Core.Enums;
using Umbraco.Forms.Core.Services;
using Umbraco.Forms.Data.Storage;

namespace umobile4contour
{

    [umClass(category:"Contour", icon: lecoati.uMobile.umComponents.GenericIcon.ListAlt)]
    public class core : uMobile
    {

        enum Range
        { 
            hour,
            Day,
            Month,
            Year,
            Decade
        };

        enum Show
        {
            five = 5,
            ten = 10,
            fifty = 50,
            thousand = 100
        };
        
        [umMethod(title:"Forms", subtitle:"Form managment", icon: lecoati.uMobile.umComponents.GenericIcon.ListAlt)]
        public static string ListForm()
        {

            FormStorage fs = new FormStorage();
            System.Collections.Generic.List<Umbraco.Forms.Core.Form> forms = fs.GetAllForms();

            List calls = new List();

            foreach (Umbraco.Forms.Core.Form form in forms)
            {    
                calls.AddListItem(new ListItem(form.Name,
                subtitle: "Created: " + form.Created.ToString(),
                icon: GenericIcon.FolderClose,
                action: new Call("ListRecord", new string[] { form.Id.ToString(), "1" }))); 
            }

            return calls.UmGo();

        }

        [umMethod(visible: false, title: "Records")]
        public static string ListRecord(String formGuid, String pageStr)
        {

            FormState state = FormState.Approved;
            Range range = Range.Day;
            Show show = Show.five;
            int page = int.Parse(pageStr);

            if (Utils.GetPostParameter("State") != null && !string.IsNullOrEmpty(Utils.GetPostParameter("State").ToString()))
                HttpContext.Current.Session["State"] = Utils.GetPostParameter("State").ToString();

            if (Utils.GetPostParameter("Range") != null && !string.IsNullOrEmpty(Utils.GetPostParameter("Range").ToString()))
                HttpContext.Current.Session["Range"] = Utils.GetPostParameter("Range").ToString();

            if (Utils.GetPostParameter("Show") != null && !string.IsNullOrEmpty(Utils.GetPostParameter("Show").ToString()))
                HttpContext.Current.Session["Show"] = Utils.GetPostParameter("Show").ToString();

            if (HttpContext.Current.Session["State"] != null && !string.IsNullOrEmpty(HttpContext.Current.Session["State"].ToString()))
                state = (FormState)Enum.Parse(typeof(FormState), HttpContext.Current.Session["State"].ToString());

            if (HttpContext.Current.Session["Range"] != null && !string.IsNullOrEmpty(HttpContext.Current.Session["Range"].ToString()))
                range = (Range)Enum.Parse(typeof(Range), HttpContext.Current.Session["Range"].ToString());

            if (HttpContext.Current.Session["Show"] != null && !string.IsNullOrEmpty(HttpContext.Current.Session["Show"].ToString()))
                show = (Show)Enum.Parse(typeof(Show), HttpContext.Current.Session["Show"].ToString());

            FormStorage fs = new FormStorage();
            RecordsViewer rv = new RecordsViewer();

            RecordStorage recordStorage = new RecordStorage();

            Umbraco.Forms.Core.Form form = fs.GetForm(Guid.Parse(formGuid));

            System.Collections.Generic.List<Record> records = rv.GetRecords(0, 0, form, Sorting.descending, new List<FormState> { state });

            List calls = new List();

            calls.AddListItem(new ListItem("<b>Filter</b>",
                subtitle: state.ToString() + ", Last " + range.ToString() + ", " + (int)show + " records per page",
                icon: GenericIcon.Filter,
                action: new Call("ConfigFilter", new string[] { formGuid })));

            DateTime minDate = DateTime.Now;
            switch (range)
            {
                case Range.hour:
                    minDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    break;

                case Range.Month:
                    minDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    break;

                case Range.Year:
                    minDate = new DateTime(DateTime.Now.Year, 1, 1);
                    break;

                case Range.Decade:
                    minDate = DateTime.MinValue;
                    break;

                case Range.Day:
                default:
                    minDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                    break;
            }

            if (records.Where(r => r.Created > minDate).Any())
            {

                int total = records.Where(r => r.Created > minDate).Count();
                IEnumerable<Record> results = records.Where(r => r.Created > minDate).OrderByDescending(r => r.Created).Skip((page - 1) * (int)show).Take((int)show);

                Button nextButton = null;
                if (total > (results.Count() + (page - 1) * (int)show))
                    nextButton = new Button(icon: GenericIcon.ArrowRight, action: new Call("ListRecord", new string[] { formGuid, (page + 1).ToString() }));

                calls.AddListItem(new ListItem("<b>Total result:</b> " + total.ToString(), subtitle: "Page <b>" + page.ToString() + "</b> to <b>" + (((int)(total / (int)show)) + 1 ).ToString() + "</b>", contextual_button: nextButton));

                foreach (Record record in results)
                {
                    calls.AddListItem(new ListItem(record.Created.ToString(),
                    subtitle: "Ip: " + record.IP,
                    action: new Call("ViewData", new string[] { record.Id.ToString() })));
                }

            }

            return calls.UmGo();

        }

        [umMethod(visible: false, title: "Filter")]
        public static string ConfigFilter(String formGuid)
        {

            FormState state = FormState.Approved;
            Range range = Range.Day;
            Show show = Show.five;

            if (HttpContext.Current.Session["State"] != null && !string.IsNullOrEmpty(HttpContext.Current.Session["State"].ToString()))
                state = (FormState)Enum.Parse(typeof(FormState), HttpContext.Current.Session["State"].ToString());

            if (HttpContext.Current.Session["Range"] != null && !string.IsNullOrEmpty(HttpContext.Current.Session["Range"].ToString()))
                range = (Range)Enum.Parse(typeof(Range), HttpContext.Current.Session["Range"].ToString());

            if (HttpContext.Current.Session["Show"] != null && !string.IsNullOrEmpty(HttpContext.Current.Session["Show"].ToString()))
                show = (Show)Enum.Parse(typeof(Show), HttpContext.Current.Session["Show"].ToString());

            lecoati.uMobile.umComponents.Form form = new lecoati.uMobile.umComponents.Form();

            form.primary = new Button("Refresh", new Call("ListRecord", new string[] { formGuid, "1" }));

            FormFieldset fieldSet = new FormFieldset(title: "Filter by");
            form.AddFieldset(fieldSet);

            SelectField selectFilter = new SelectField("State", "State", state.ToString());
            foreach (FormState stateIndex in Enum.GetValues(typeof(FormState)))
                selectFilter.AddOption(stateIndex.ToString(), stateIndex.ToString());
            fieldSet.AddFormItem(selectFilter);

            SelectField selectRange = new SelectField("Range", "Range", range.ToString());
            foreach (Range rangeIndex in Enum.GetValues(typeof(Range)))
                selectRange.AddOption(rangeIndex.ToString(), rangeIndex.ToString());
            fieldSet.AddFormItem(selectRange);

            FormFieldset fieldSetEntries = new FormFieldset(title: "Entries");
            form.AddFieldset(fieldSetEntries);

            SelectField selecShow = new SelectField("Show", "Show", ((int)show).ToString());
            foreach (Range showIndex in Enum.GetValues(typeof(Show)))
                selecShow.AddOption(((int)showIndex).ToString(), showIndex.ToString());
            fieldSetEntries.AddFormItem(selecShow);

            return form.UmGo();

        }

        [umMethod(visible: false, title: "Record detail")]
        public static string ViewData(String recordId)
        {

            FormStorage fs = new FormStorage();
            RecordStorage rs = new RecordStorage();

            Record record = rs.GetRecord(Guid.Parse(recordId));

            Umbraco.Forms.Core.Form form = fs.GetForm(record.Form);

            List calls = new List();

            calls.AddListItem(new ListItem("<b>Approve</b>",
                subtitle: "Approve record",
                icon: GenericIcon.Check,
                action: new Call("CheckRecordConfirm", new string[] { recordId })));

            calls.AddListItem(new ListItem("<b>Delete</b>",
                subtitle: "Delete record",
                icon: GenericIcon.Trash,
                action: new Call("DeleteRecordConfirm", new string[] { recordId })));

            calls.AddListItem(new ListItem("<b>Email</b>",
                subtitle: "Email record",
                icon: GenericIcon.EnvelopeAlt,
                action: new Call("EmailRecordForm", new string[] { recordId })));

            String data = string.Empty;

            data += "<p><b>State:</b><br/>" + record.State.ToString() + "</p><br />";
            data += "<p><b>Created:</b><br/>" + record.Created.ToString() + "</p><br />";
            data += "<p><b>Ip:</b><br/>" + record.IP + "</p><br />";

            foreach (Field field in form.AllFields)
            {
                string value = string.Empty;
                if (record.GetRecordField(field.Id) != null && record.GetRecordField(field.Id).Values.Count > 0)
                {
                    value = record.GetRecordField(field.Id).Values[0].ToString();
                }
                data += "<p><b>" + field.Caption + ":</b><br/>" + value + "</p><br />";
            }

            calls.AddListItem(new ListItem("<b>Record</b><br/><br />",
               subtitle: data));

            return calls.UmGo();

        }

        [umMethod(visible: false, title: "Approve Record Confirm")]
        public static string CheckRecordConfirm(String recordId)
        {
            return new MessageBox("Are you sure you want to approve this record", new Button("Ok", new Call("CheckRecord", new string[] { recordId })), new Button("Cancel"))
                .UmGo();
        }

        [umMethod(visible: false, title: "Approve Record")]
        public static string CheckRecord(String recordId)
        {

            try
            {
                RecordStorage rs = new RecordStorage();
                Record record = rs.GetRecord(Guid.Parse(recordId));
                RecordService s = new RecordService(record);
                s.Approve();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("This license is invalid"))
                    return new MessageBox("Record approved successfully", new Button("Ok", new Call("ViewData", new string[] { recordId }))).UmGo();
                else
                    return new MessageBox("Error to approved this record", new Button("Close")).UmGo("Warning");
            }

            return new MessageBox("Record approved successfully", new Button("Ok", new Call("ViewData", new string[] { recordId })))
                .UmGo();

        }

        [umMethod(visible: false, title: "Delete Record Confirm")]
        public static string DeleteRecordConfirm(String recordId)
        {
            return new MessageBox("Are you sure you want to delete this record", new Button("Ok", new Call("DeleteRecord", new string[] { recordId })), new Button("Cancel"))
                .UmGo();
        }

        [umMethod(visible: false, title: "Delete Record")]
        public static string DeleteRecord(String recordId)
        {

            string formGuid = null;

            try
            {
                RecordStorage rs = new RecordStorage();
                Record record = rs.GetRecord(Guid.Parse(recordId));
                RecordService s = new RecordService(record);
                formGuid = record.Form.ToString();
                s.Delete();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("This license is invalid"))
                    return new MessageBox("Record deleted successfully", new Button("Ok", new Call("ListRecord", new string[] { formGuid, "1" }))).UmGo();
                else
                    return new MessageBox("Error to delete this record", new Button("Close")).UmGo("Warning");
            }

            return new MessageBox("Record deleted successfully", new Button("Ok", new Call("ListRecord", new string[] { formGuid, "1" })))
                .UmGo();

        }

        [umMethod(visible: false, title: "Email record Form")]
        public static string EmailRecordForm(String recordId)
        {

            TextField tFEmail = new TextField("email", "Email");

            lecoati.uMobile.umComponents.Form form = new lecoati.uMobile.umComponents.Form();

            form.primary = new Button("Send", new Call("EmailRecord", new string[] { recordId }));

            FormFieldset fieldSet = new FormFieldset(title: "Filter by");
            form.AddFieldset(fieldSet);

            fieldSet.AddFormItem(tFEmail);            

            return form.UmGo();

        }

        [umMethod(visible: false, title: "Email record")]
        public static string EmailRecord(String recordId)
        {

            String emailTo = Utils.GetPostParameter("email").ToString();
            String emailFrom = umbraco.UmbracoSettings.NotificationEmailSender;

            FormStorage fs = new FormStorage();
            RecordStorage rs = new RecordStorage();

            Record record = rs.GetRecord(Guid.Parse(recordId));

            Umbraco.Forms.Core.Form form = fs.GetForm(record.Form);

            string data = "<p><b>Form:</b> " + form.Name + "</p><br />";
            data += "<p><b>State:</b><br/>" + record.State.ToString() + "</p><br />";
            data += "<p><b>Created:</b><br/>" + record.Created.ToString() + "</p><br />";
            data += "<p><b>Ip:</b><br/>" + record.IP + "</p><br />";

            foreach (Field field in form.AllFields)
            {
                string value = string.Empty;
                if (record.GetRecordField(field.Id) != null && record.GetRecordField(field.Id).Values.Count > 0)
                {
                    value = record.GetRecordField(field.Id).Values[0].ToString();
                }
                data += "<p><b>" + field.Caption + ":</b><br/>" + value + "</p><br />";
            }

            try
            {
                umbraco.library.SendMail(emailFrom, emailTo, "Contour record", data, true);
            }
            catch
            {
                return new MessageBox("Error to send record ", new Button("Close")).UmGo("Warning");
            }

            return new MessageBox("Record sent successfully", new Button("Ok", new Call("ViewData", new string[] { recordId })))
                .UmGo();

        }

    }
}


