/********************************************************************************
* Copyright Sysmates Pte Limited
*******************************************************************************/

var __IsPageDirty = null;
var deletedFormDatas = new Array();
var TransactionIds = new Array();
var ContainerForms = new Array();
var ChangeFormIds = new Array();
var CallerForms = new Array();
var AutoCompleteFieldValues = new Array();
var DependentFields = new Object();

var sModalWindowFeatures = { dialogHeight: 450, dialogWidth: 650, status: 'no', resizable: 1 };

function Evaluate(obj) { return eval("(" + obj + ")") }

function getKeycode(event) {
    keycode = (event.keyCode ? event.keyCode : (event.which ? event.which : event.charCode));
    //if (keycode == 27)
    //    return true;
    return false;
}

function AppPressReady() {
    $(window).scrollTop();
    $(document).bind("keydown", function (event) {
        keycode = (event.keyCode ? event.keyCode : (event.which ? event.which : event.charCode));
        if (keycode == 27) {
            event.preventDefault();
        }
    });
    //$('[id^=AppPress_]').focusin(function () {
    //    $(this).attr('oldValue', $(this).val());
    //})

    SetDirty(false);
    $(window).bind("beforeunload", function () {
        try {
            if (GetDirty() && typeof (WarnOnDirtyClose) != "undefined") {
                return 'You have some un-saved data in this page, do you want to continue with out saving those data?';
            }

        } catch (e) {

        }
    });

}

function Status() {
    var statusObj = new Object();
    statusObj.continueExecution = true;
    return statusObj;
}

function addRow(id, cellContent, tdAttr, trId) {
    {

        if (tdAttr != null) {
            rowStrU += "<td " + tdAttr + ">";
            rowStrL = "</td></tr>"
        }
        $('#' + id).find("TBODY").first().append(cellContent);

    }
}

function GetFieldIndex(formDef, fieldName) {
    for (var j = 0; j < formDef.formFields.length; ++j) {
        if (formDef.formFields[j].fieldName == fieldName)
            break;
    }
    if (j >= formDef.formFields.length)
        alert("Internal Error: Could not Find Field:" + fieldName + " in Form:" + formDef.formName);
    return j;
}

function FindFormDef(formName) {

    var ___formDefs = __formDefs;
    if (!___formDefs)
        debugger;
    for (var i = 0; i < ___formDefs.length; ++i) {
        if (___formDefs[i].formName == formName)
            return ___formDefs[i];
    }

    debugger;
    return null;
}

function stringifyP(pageData) {
    return escape(JSON.stringify(pageData));
}
var popupDialogs = new Array(), $alertDialog, $promptDialog;

function _ProcessClientActions(pageData, clientActions, startIndex) {
    for (var i = startIndex; i < clientActions.length; ++i) {
        var ca = clientActions[i];
        switch (ca.clientActionType.toString()) {

            case '20': //PromptClient
                $promptDialog = $('<div></div>')
		.html(ca.message)
		.dialog({
		    autoOpen: false,
		    modal: true,
		    close: function (event, ui) {
		        $(this).dialog('destroy').remove();
		    },
		    buttons: {
		        "Ok": function () {
		            result = true;
		            $(this).dialog("close");
		            AjaxFunctionCall(pageData.functionCall, ca.formName, ca.id, pageData.fieldName, this, result)
		        },
		        "Cancel": function () {
		            $(this).dialog("close");
		        }
		    }
		});

                $promptDialog.dialog('open');
                break;
            case '17': //FormError
                var message = ca.message;
                var obj = $("#_FormError_" + ca.formName + "_" + ca.id);
                if (obj.length == 0) {
                    message = " Error in Form:" + ca.formName + " with Id: " + ca.id + "\n" + message;
                    alert(message + "\n\nCould not find HTML Element _FormError_" + ca.formName + "_" + ca.id);
                    break;
                }
                else {
                    var containerObj = $("#_FormErrorContainer_" + ca.formName + "_" + ca.id);
                    if (containerObj.length > 0) {
                        containerObj.toggle(true);
                    }
                }
                obj.html(message);
                break;
            case '19': //SetPageNonDirty
                SetDirty(false);
                break;
            case '1': //AlertMessage
                $alertDialog = $('<div></div>')
		.html(ca.message)
		.dialog({
		    autoOpen: false,
		    modal: true,
		    width: 300,
		    title: ca.popupTitle == null ? "Alert" : ca.popupTitle,
		    close: function (event, ui) {
		        $(this).dialog('destroy').remove();
		        _ProcessClientActions(pageData, clientActions, i + 1);
		    }
		});

                $alertDialog.dialog('open');
                return;
            case '9': //PageRefresh
                __IsPageDirty = false;
                location.href = location.href;
                break;
            case '13': //CloseWindow
                var popupDialog = popupDialogs[popupDialogs.length - 1];
                popupDialog.popupDialog.dialog('destroy').remove();
                popupDialogs.pop();
                break;
            case '3': //ClearErrors
                if (ca.fieldName) {
                    var id = 'error_' + ca.fieldName;
                    var obj = $('#' + id);
                    if (obj.length == 1)
                        obj.html('');
                }
                else {
                    $("[id^=error_]").html('');
                    try {
                        $("#_FormErrorContainer").toggle(false);
                    }
                    catch (e) { }
                    $("#_FormError").html('');
                }
                break;
            case '4': //Popup
                var $popupDialog = $('<div></div>');
                var popupDialog = new Object();
                popupDialog.popupDialog = $popupDialog;
                popupDialog.rootFormName = ca.formName;
                popupDialog.IsPageDirty = false;
                popupDialog.deletedFormDatas = new Array();
                popupDialog.ChangeFormIds = new Array();
                popupDialogs.push(popupDialog);
                $popupDialog.html(ca.fieldHtml);
                $popupDialog.dialog({
                    autoOpen: false,
                    modal: true,
                    width: ca.popupWidth,
                    height: ca.popupHeight,
                    title: ca.popupTitle,
                    position: ["center", 20],
                    open: function (event, ui) {
                        if (ca.id > 0) {
                            document.activeElement.blur();
                            document.documentElement.focus(); // to make escape work in begining
                        }
                    },
                    close: function (event, ui) {
                        var popupDialog = popupDialogs[popupDialogs.length - 1];
                        popupDialog.popupDialog.dialog('destroy').remove();
                        popupDialogs.pop();
                    },
                    beforeClose: function (event, ui) {
                        var popupDialog = popupDialogs[popupDialogs.length - 1];
                        if (popupDialog.IsPageDirty) {
                            if (typeof (WarnOnDirtyClose) != "undefined")
                                if (!confirm("This dialog is unsaved. Do you really want to close it?"))
                                    return false;
                        }
                    }
                });

                $popupDialog.dialog('open');
                if (ca.JsStr)
                    eval(ca.JsStr);
                if (ca.readonlyStr) {
                    eval(ca.readonlyStr);
                    popupDialog.readonlyStr = ca.readonlyStr;
                }
                return;
            case '7': //Redirect
                if (!ca.warnOnDirtyClose) // session expired redirect should not show warn
                    __IsPageDirty = false;
                try {
                    location.href = ca.url;
                }
                catch (e) {
                    // jQuery has bug in this case for window closing event
                }
                break;
            case '11': //SetFieldValue
                var formDef = FindFormDef(ca.formName);
                var fieldIndex = GetFieldIndex(formDef, ca.fieldName);
                var fieldType = formDef.formFields[fieldIndex].Type.toString();
                switch (fieldType) {
                    case '3': //TextArea
                    case '1': //Text
                    case '5': //Password
                    case '2': //Number
                    case '9': FileUpload
                        var obj = $("#AppPress_" + fieldType + "_" + formDef.formName + '_' + ca.fieldName + '_' + ca.id);
                        obj.val(ca.Value);
                        break;
                    case '7': //Pickone
                        var style = formDef.formFields[fieldIndex].Style.toString();
                        if (style == '5') // Radio
                            $("[Name='" + objId + "']:checked").val(ca.Value);
                        else if (style == '7') { // Imagemap
                            var obj = $("#AppPress_" + fieldType + "_" + ca.formName + '_' + ca.fieldName + '_' + ca.id);
                            obj[0].attributes['value'].value = ca.Value;
                        }
                        else if (style == '9') { // AutoComplete
                            var obj = $("#AppPress_" + fieldType + "_" + ca.formName + '_' + ca.fieldName + '_' + ca.id);
                            obj.val(ca.fieldHtml);
                            AutoCompleteFieldValues[ca.formName + '_' + ca.fieldName + '_' + ca.id] = ca.Value;
                        }
                        else if (style == '10') { // StaticTextWithId
                            var obj = $("#AppPress_" + fieldType + "_" + ca.formName + '_' + ca.fieldName + '_' + ca.id);
                            obj.html(ca.fieldHtml);
                            AutoCompleteFieldValues[ca.formName + '_' + ca.fieldName + '_' + ca.id] = ca.Value;
                        }
                        else { // Dropdown
                            var obj = $("#AppPress_" + fieldType + "_" + ca.formName + '_' + ca.fieldName + '_' + ca.id);
                            obj.val(ca.Value);
                        }
                        SetDirty();
                        break;
                    case '4': //FormContainer
                        var obj = $("#fieldContainer_" + formDef.formName + '_' + ca.fieldName + '_' + ca.id);
                        obj.html(ca.fieldHtml);
                        break;
                    case '12': //Image
                        var obj = $("#AppPress_" + fieldType + "_" + formDef.formName + '_' + ca.fieldName + '_' + ca.id);
                        obj[0].src = ca.Value;
                        break;
                    case '8': //Checkbox
                        if (ca.id == null)
                            $('input|checkbox[id*="' + ca.formName + '_' + ca.fieldName + '_"]').attr('checked', ca.Value == '1');
                        else {
                            var obj = $("#AppPress_" + fieldType + "_" + formDef.formName + '_' + ca.fieldName + '_' + ca.id);
                            if (ca.Value == "1")
                                obj.attr("checked", "checked");
                            else
                                obj.removeAttr("checked");
                        }
                        break;
                }
                break;
            case '14': //SetFocus
                var formDef = FindFormDef(ca.formName);
                var fieldIndex = GetFieldIndex(formDef, ca.fieldName);
                var objSetFocus = $("#AppPress_" + formDef.formFields[fieldIndex].Type.toString() + "_" + ca.formName + '_' + ca.fieldName + '_' + ca.id);
                setTimeout(
                        function () {
                            try {
                                objSetFocus.focus().select();
                            }
                            catch (e) {
                                // ignore errors
                            }
                        });
                break;
            case '2': //FieldError
                var id = 'error_' + ca.fieldName;
                var obj = $('#' + id);
                if (obj.length == 0) {
                    alert(ca.message + "\n\nInternal Error: Could not find " + id);
                }
                else
                    obj.html(ca.message);
                break;

            case '5': //RefreshField
                var formDef = FindFormDef(ca.formName);
                var id = 'fieldContainer_' + ca.formName + '_' + ca.fieldName + '_' + ca.id;
                var obj = $('#' + id);
                if (obj.length == 0) {
                    alert("Internal Error: Could not find " + id);
                }
                else {
                    obj.html(ca.fieldHtml);
                    if (ca.JsStr)
                        eval(ca.JsStr);
                    if (ca.readonlyStr)
                        eval(ca.readonlyStr);
                }
                break;
            case '8': // AddInlineRow
                // ca.formName -- form in which the grid is contained in
                // ca.fieldName -- fieldName in the form for the grid
                // ca.id -- id of the added row
                // ca.fieldHtml -- row content to be added
                var containerId = "rowContainer_" + ca.formName + "_" + ca.fieldName + "_" + ca.id;
                var containerObj = $('#' + containerId);
                if (containerObj.length == 0)
                    alert("Could not find:" + containerId);
                else {
                    containerObj.append(ca.fieldHtml);
                    try {
                        $("#" + formDef.formFields[j].formDef.formName + '_' + formDef.formFields[j].formDef.formFields[0].fieldName + '_' + ca.id).focus();
                    }
                    catch (Error) { }
                    SetDirty(true);
                }
                break;

            case '22': //SetPageDirty
                SetDirty(true);
                break;
            case '10': //DeleteRow

                formDef = FindFormDef(ca.formName);
                var j = GetFieldIndex(formDef, ca.fieldName);

                var rowId = "row_" + formDef.formName + "_" + ca.fieldName + "_" + ca.id;
                var currentRowElement = $('#' + rowId);
                if (currentRowElement.length == 0) {
                    alert("Could not find row container with Id:" + rowId);
                    break;
                }
                var nextRowElement = currentRowElement.next();
                var formData = new Object();
                formData.formName = formDef.formFields[j].ContainerRowForm;
                formData.id = ca.id;
                currentRowElement.remove();
                var topDeletedFormDatas = popupDialogs.length == 0 ? deletedFormDatas : popupDialogs[popupDialogs.length - 1].deletedFormDatas;
                topDeletedFormDatas.push(formData);
                SetDirty(true);
                break;
            case '16': //OpenUrl
                jQuery('<form action="' + ca.url + '" method="post"></form>')
    .appendTo('body').submit().remove();

                break;
            case '18': //UpdateTransactionId
                $("#" + ca.fieldName).val(ca.TransactionId);
                break;
            case '21': //SetFormDataAsDeleted

                var formData = new Object();
                formData.formName = ca.formName;
                formData.id = ca.id;
                var topDeletedFormDatas = popupDialogs.length == 0 ? deletedFormDatas : popupDialogs[popupDialogs.length - 1].deletedFormDatas;
                topDeletedFormDatas.push(formData);
                SetDirty(true);
                break;
            case '23': // ChangeFormId
                var topChangeFormIds = popupDialogs.length == 0 ? ChangeFormIds : popupDialogs[popupDialogs.length - 1].ChangeFormIds;
                var formData = new Object();
                formData.formName = ca.formName;
                formData.id = ca.id;
                formData.newId = ca.Value;
                topChangeFormIds[topChangeFormIds.length] = formData;
                break;
            default:
                alert("Internal Error: Could not find ClientActionType:" + ca.clientActionType);
                break;
        }
    }
}
function ProcessClientActions(pageData) {
    if (pageData.appPressResponse != null) {
        var clientActions = pageData.appPressResponse;
        pageData.appPressResponse = null;
        _ProcessClientActions(pageData, clientActions, 0)
    }
}

function OnChange(obj, always) {

    pageData.RefreshFields = new Array();
    $.each(DependentFields,
        function(index,value){
            $.each(value,function(index1,value1){
                if (value1 == obj.id)
                    pageData.RefreshFields.push(index)
            })
        })

    if (always || pageData.RefreshFields.length > 0)
    {
        var s = obj.id.split("_");
        if (s.length < 5) {
            alert("Invalid Object Id:" + obj.id);
        }
        var formFieldType = s[1];
        var formName = s[2];
        var fieldName = s[3];
        var formId = s[4];
        AjaxFunctionCall("OnChange",formName,formId,fieldName,obj);
    }

}

function GetFormData(pageData, clickEvent) {
    // Scan all fields and Create a new FormData list
    var evn = null;
    if (clickEvent)
        evn = clickEvent;
    else if (this.event && this.event.type == "click")
        evn = this.event;

    pageData.formDatas = new Array();
    var objs = $("[id^=AppPress_]");
    for (var ii = 0; ii < objs.length; ++ii) {
        var obj = $(objs[ii]);
        var s = obj[0].id.split("_");
        if (s.length < 5) {
            alert("Invalid Object Id:" + obj[0].id);
        }
        var objsDup = $("[id=" + obj[0].id + "]");
        if (objsDup.length > 1)
            alert("Found 2 objects with Id: " + obj[0].id);
        var formFieldType = s[1];
        var formDefName = s[2];
        var fieldName = s[3];
        var formDataId = s[4];
        var formDef = FindFormDef(formDefName, this);
        var formData = null
        for (var j = 0; j < pageData.formDatas.length; ++j)
            if (pageData.formDatas[j].formName == formDefName && pageData.formDatas[j].id == formDataId) {
                formData = pageData.formDatas[j];
                break;
            }
        if (formData == null) {
            formData = new Object();
            formData.formName = formDefName;
            formData.id = formDataId;
            var formDef = FindFormDef(formDefName, this);
            if (TransactionIds[formData.formName + "_" + formData.id])
                formData.TransactionId = TransactionIds[formData.formName + "_" + formData.id];
            var containerForm = ContainerForms[formData.formName + "_" + formData.id];
            if (containerForm) {
                formData.containerFieldValue = new Object();
                formData.containerFieldValue.formField = new Object();
                formData.containerFieldValue.formField.fieldName = containerForm.fieldName;
                formData.containerFieldValue.formData = new Object();
                formData.containerFieldValue.formData.id = containerForm.formDataId;
                formData.containerFieldValue.formData.formName = containerForm.formName;
            }
            if (CallerForms[formData.formName + "_" + formData.id]) {
                formData.callerFieldValue = new Object();
                formData.callerFieldValue.formField = new Object();
                formData.callerFieldValue.formField.fieldName = CallerForms[formData.formName + "_" + formData.id].fieldName;
                formData.callerFieldValue.formData = new Object();
                formData.callerFieldValue.formData.id = CallerForms[formData.formName + "_" + formData.id].formDataId;
                formData.callerFieldValue.formData.formName = CallerForms[formData.formName + "_" + formData.id].formName;
            }
            formData.fieldValues = new Array();
            pageData.formDatas.push(formData);
        }
        switch (formFieldType) {
            case '3': // FormDefFieldType.TextArea
            case '1': // FormDefFieldType.Text
            case '5': // FormDefFieldType.Password
            case '2': // FormDefFieldType.Number
            case '10': // FormDefFieldType.DateTime
                {
                    var fieldValueIndex = formData.fieldValues.length;
                    formData.fieldValues[fieldValueIndex] = new Object();
                    formData.fieldValues[fieldValueIndex].formField = new Object();
                    formData.fieldValues[fieldValueIndex].formField.fieldName = fieldName;
                    formData.fieldValues[fieldValueIndex].Value = obj.val() == '' ? null : obj.val();
                    formData.fieldValues[fieldValueIndex].ReadOnly = obj.attr('disabled') == 'disabled' ? 1 : 0;
                    break;
                }
            case '8': // FormDefFieldType.Checkbox
                {
                    var fieldValueIndex = formData.fieldValues.length;
                    formData.fieldValues[fieldValueIndex] = new Object();
                    formData.fieldValues[fieldValueIndex].formField = new Object();
                    formData.fieldValues[fieldValueIndex].formField.fieldName = fieldName;
                    formData.fieldValues[fieldValueIndex].Value = obj.attr("checked") == "checked" ? "1" : "0";
                    formData.fieldValues[fieldValueIndex].ReadOnly = obj.attr('disabled') == 'disabled' ? 1 : 0;
                    break;
                }
            case '7': // FormDefFieldType.Pickone
                {
                    var formFieldIndex = GetFieldIndex(formDef, fieldName);
                    var style = formDef.formFields[formFieldIndex].Style.toString();
                    if (style == '5') { // Radio
                        for (var fieldValueIndex = 0; fieldValueIndex < formData.fieldValues.length; ++fieldValueIndex)
                            if (formData.fieldValues[fieldValueIndex].formField.fieldName == fieldName)
                                break;
                        if (fieldValueIndex == formData.fieldValues.length) {
                            formData.fieldValues[fieldValueIndex] = new Object();
                            formData.fieldValues[fieldValueIndex].formField = new Object();
                            formData.fieldValues[fieldValueIndex].formField.fieldName = fieldName;
                            formData.fieldValues[fieldValueIndex].Value = null;
                        }
                        if (obj.attr("checked") == 'checked')
                            formData.fieldValues[fieldValueIndex].Value = obj.val();
                    }
                    else if (style == '9' || style == '10') { // AutoComplete || StaticTextWithId
                        var fieldValueIndex = formData.fieldValues.length;
                        formData.fieldValues[fieldValueIndex] = new Object();
                        formData.fieldValues[fieldValueIndex].formField = new Object();
                        formData.fieldValues[fieldValueIndex].formField.fieldName = fieldName;
                        formData.fieldValues[fieldValueIndex].Value = AutoCompleteFieldValues[formDefName + "_" + fieldName + "_" + formDataId];
                    }
                    else if (style == '7') { // ImageMap
                        var fieldValueIndex = formData.fieldValues.length;
                        formData.fieldValues[fieldValueIndex] = new Object();
                        formData.fieldValues[fieldValueIndex].formField = new Object();
                        formData.fieldValues[fieldValueIndex].formField.fieldName = fieldName;
                        formData.fieldValues[fieldValueIndex].Value = obj[0].attributes['value'].value;
                        if (evn != null) {
                            var id;
                            if (evn.srcElement)
                                id = evn.srcElement.id;
                            else
                                id = evn.currentTarget.id;
                            if (id == obj[0].id) {
                                var of = obj.offset();
                                pageData.eventData.offsetX = parseInt(evn.clientX - of.left);
                                pageData.eventData.offsetY = parseInt(evn.clientY - of.top);
                            }
                        }

                    }
                    else {
                        var fieldValueIndex = formData.fieldValues.length;
                        formData.fieldValues[fieldValueIndex] = new Object();
                        formData.fieldValues[fieldValueIndex].formField = new Object();
                        formData.fieldValues[fieldValueIndex].formField.fieldName = fieldName;
                        formData.fieldValues[fieldValueIndex].Value = obj.val();
                        if (formData.fieldValues[fieldValueIndex].Value == "")
                            formData.fieldValues[fieldValueIndex].Value = null;
                        var metaData = "";
                        obj.find('option').each(function () {
                            metaData += this.value + ",";
                        });
                        formData.fieldValues[fieldValueIndex].MetaData = metaData;
                        formData.fieldValues[fieldValueIndex].ReadOnly = obj.attr('disabled') == 'disabled' ? 1 : 0;
                    }

                    break;
                }
            case '9': // FormDefFieldType.FileUpload
                {
                    var fieldValueIndex = formData.fieldValues.length;
                    formData.fieldValues[fieldValueIndex] = new Object();
                    formData.fieldValues[fieldValueIndex].formField = new Object();
                    formData.fieldValues[fieldValueIndex].formField.fieldName = fieldName;

                    formData.fieldValues[fieldValueIndex].Value = obj.attr("FileId");
                    if (typeof (formData.fieldValues[fieldValueIndex].Value) == "undefined" || formData.fieldValues[fieldValueIndex].Value == '')
                        formData.fieldValues[fieldValueIndex].Value = null;
                    break;
                }
            case '4': // FormDefFieldType.FormContainer
                {
                    break;
                }
            case '6': // FormDefFieldType.Button
                {
                    break;
                }
        }
    }

    for (kk = 0; kk <= popupDialogs.length; ++kk) {
        var topDeletedFormDatas = kk == popupDialogs.length ? deletedFormDatas : popupDialogs[kk].deletedFormDatas;
        for (ii = 0; ii < topDeletedFormDatas.length; ++ii) {
            var dFormData = null;
            for (var jj = 0; jj < pageData.formDatas; ++jj)
                if (pageData.formDatas[jj].formName == topDeletedFormDatas[ii].formName && pageData.formDatas[jj].id == topDeletedFormDatas[ii].id) {
                    dFormData = pageData.formDatas[jj];
                    break;
                }
            if (dFormData == null) {
                dFormData = topDeletedFormDatas[ii];
                pageData.formDatas.push(dFormData);
            }
            dFormData.IsDeleted = true;
            var containerForm = ContainerForms[dFormData.formName + "_" + dFormData.id];
            if (containerForm) {
                dFormData.containerFieldValue = new Object();
                dFormData.containerFieldValue.formField = new Object();
                dFormData.containerFieldValue.formField.fieldName = containerForm.fieldName;
                dFormData.containerFieldValue.formData = new Object();
                dFormData.containerFieldValue.formData.id = containerForm.formDataId;
                dFormData.containerFieldValue.formData.formName = containerForm.formName;
            }
        }
    }
}

var clickEvent = null;
function CatchEvent(event) {
    clickEvent = event;
}

function AjaxFunctionCall(functionName, formName, formId, fieldName, sender, result, columnName) {
    if (sender && sender.attributes && sender.attributes['disabled'] && sender.attributes['disabled'].value == 'disabled') // could not do this with jQuery
        return false; // for disabling links
    pageData.screenWidth = screen.width;
    pageData.screenHeight = screen.height;
    pageData.documentWidth = $(document).width();
    pageData.documentHeight = $(document).height();
    pageData.windowWidth = $(window).width();
    pageData.windowHeight = $(window).height();
    // get clientChanges
    GetFormData(pageData, clickEvent);
    pageData.appPressResponse = null;
    pageData.rootFormData = new Object();
    pageData.rootFormData.formName = rootFormName;
    pageData.rootFormData.id = rootFormId;
    pageData.fieldValue = new Object();
    pageData.fieldValue.formData = new Object();
    pageData.fieldValue.formData.formName = formName;
    pageData.fieldValue.formData.id = formId;
    pageData.fieldName = fieldName;
    pageData.result = result;
    pageData.columnName = columnName;
    pageData.pageTimeStamp = pageTimeStamp;
    pageData.ChangeFormIDs = popupDialogs.length == 0 ? ChangeFormIds : popupDialogs[popupDialogs.length - 1].ChangeFormIds;
    if (pageData.ChangeFormIDs.length == 0)
        pageData.ChangeFormIDs = null;
    var pageurl = unescape(baseUrl) + 'Index.Aspx';
    sender = $(sender);
    sender.attr('disabled', 'disabled');
    var s = stringifyP(pageData);
    pageurl += "?functionCall=" + functionName;
    $.ajax({
        type: 'POST', url: pageurl, data: { 'pageData': s },
        success: function (result) {
            var pageData1 = eval('(' + result + ')');
            ProcessClientActions(pageData1);
            sender.removeAttr('disabled');
            //sender.val(sender.attr('oldValue'));
        },
        error: function (obj, errorStr, errorThrown) {
            if (errorThrown)
                errorStr += "\n" + errorThrown.name + ":" + errorThrown.message + " in ajax call to " + functionName;

            alert(errorStr);
            sender.removeAttr('disabled');
            //if (sender.attr('oldValue'))
            //    sender.val(sender.attr('oldValue'));
        }

    });
}

function AutoCompleteCall(formName, formId, fieldName, req, add) {

    pageData.screenWidth = screen.width;
    pageData.screenHeight = screen.height;
    pageData.documentWidth = $(document).width();
    pageData.documentHeight = $(document).height();
    pageData.windowWidth = $(window).width();
    pageData.windowHeight = $(window).height();
    // get clientChanges
    GetFormData(pageData, clickEvent);
    pageData.appPressResponse = null;
    pageData.rootFormData = new Object();
    pageData.rootFormData.formName = rootFormName;
    pageData.rootFormData.id = rootFormId;
    pageData.fieldValue = new Object();
    pageData.fieldValue.formData = new Object();
    pageData.fieldValue.formData.formName = formName;
    pageData.fieldValue.formData.id = formId;
    pageData.fieldName = fieldName;
    pageData.autoCompleteTerm = req.term;

    var s = stringifyP(pageData);
    var pageurl = unescape(baseUrl) + 'Index.Aspx';
    pageurl += "?functionCall=AutoCompleteOptions";
    $.ajax({
        type: 'POST', url: pageurl, data: { 'pageData': s },
        success: function (result) {
            var pageData1 = eval('(' + result + ')');
            add(pageData1.autoCompleteOptions);
        },
        error: function (obj, errorStr, errorThrown) {
            if (errorThrown)
                errorStr += "\n" + errorThrown.name + ":" + errorThrown.message + " in ajax call to " + functionName;

            alert(errorStr);
        }

    });
}

function GetTimeStamp() {
    var date = new Date();
    return date.getDay() + "" + date.getMonth() + "" + date.getYear() + "" + date.getHours() + "" + date.getMinutes() + "" + date.getSeconds();
}


function OpenFileWindow(sender) {
    this.open($(sender).attr('src') + '&Download=true', '');

}

function SetElementHtml(elementId, markup) {
    var decodedMarkup = unescape(markup);
    $('#' + elementId).append(decodedMarkup);

}

function CheckUncheckAll(obj, idPrefix) {

    if ($(obj).attr("checked") == "checked")
        $("[type='checkbox'][id^='" + idPrefix + "']").not(obj).attr("checked", "checked");
    else
        $("[type='checkbox'][id^='" + idPrefix + "']").not(obj).removeAttr("checked");

}

function SetFormContainerPage(query, first, formDefName, formDataId, fieldName, pageObj) {
    var id = 'girdContainer_' + formDefName + "_" + fieldName + "_" + formDataId;
    var obj = $('#' + id);
    if (obj.length == 0) {
        alert("Internal Error: Could not find object with" + id);
    }
    else {
        var pageurl = unescape(baseUrl) + 'FunctionCall.Aspx';
        var formDef = FindFormDef(formDefName);
        pageData.formDefName = formDefName;
        pageData.formDataId = formDataId;
        pageData.fieldIndex = GetFieldIndex(formDef, fieldName);
        GetFormData(pageData);
        var formData = FindFormData(formDefName, formDataId, pageData.formDatas);
        formData.fieldValues[pageData.fieldIndex].paging = new Object();
        formData.fieldValues[pageData.fieldIndex].paging.Query = unescape(query);
        formData.fieldValues[pageData.fieldIndex].paging.First = first;
        var s = stringifyP(pageData);
        $(pageObj).attr('disabled', 'disabled');
        $.ajax({
            type: 'POST', url: pageurl + "?functionCall=GetFormContainerPageDataAndHtml", data: { 'pageData': s },
            success: function (result) {
                if (typeof (result) != "undefined") {
                    var p1 = eval('(' + result + ')');
                    pageData.formData = p1.formData;
                    $(obj).html(p1.fieldHtml);
                    $(pageObj).removeAttr('disabled');
                }
            },
            error: function (obj, errorStr, errorThrown) {
                if (errorThrown)
                    errorStr += "\n" + errorThrown.name + ":" + errorThrown.message + " in ajax call to " + functionName;
                alert(errorStr);
                $(pageObj).removeAttr('disabled');
            }
        });
    }
}

function GetDirty() {
    return popupDialogs.length == 0 ? __IsPageDirty : popupDialogs[popupDialogs.length - 1].IsPageDirty;
}

function SetDirty(dirty) {
    if (typeof (dirty) == "undefined")
        dirty = true;
    var pageDirty = GetDirty();
    if (pageDirty != dirty) {
        if (popupDialogs.length == 0) {
            __IsPageDirty = dirty;
            eval(readonlyStr);
        }
        else {
            popupDialogs[popupDialogs.length - 1].IsPageDirty = dirty;
            eval(popupDialogs[popupDialogs.length - 1].readonlyStr);
        }
    }
}

function FindFormData(formName, rowId, formDatas) {
    for (var i = 0; i < formDatas.length; ++i)
        if (formDatas[i].formName == formName && formDatas[i].id == rowId)
            return formDatas[i];
    return null;
}

function SetFormData(pFormData, formName, rowId, formData) {
    for (var i = 0; i < pFormData.fieldValues.length; ++i)
        if (pFormData.fieldValues[i] != null && pFormData.fieldValues[i].containedForms != null)
            for (var j = 0; j < pFormData.fieldValues[i].containedForms.length; ++j) {
                if (pFormData.fieldValues[i].containedForms[j].formName == formName && pFormData.fieldValues[i].containedForms[j].id == rowId) {
                    pFormData.fieldValues[i].containedForms[j].formData = formData;
                    return true;
                }
                if (SetFormData(formData.fieldValues[i].containedForms[j].formData, formName, formData))
                    return true;
            }
    return false;
}

function DeleteGridRow(formName, fieldName, id) {
    var formDef = FindFormDef(formName);
    for (var j = 0; j < formDef.formFields.length; ++j) {
        if (formDef.formFields[j].fieldName == fieldName) {
            GetFormData(pageData);
            pageData.appPressResponse = new Array();
            var ca = new Object();
            ca.id = id;
            ca.clientActionType = '10';
            ca.fieldName = fieldName;
            ca.formName = formName;
            pageData.appPressResponse.push(ca);
            this.ProcessClientActions(pageData)
            return;
        }
    }
    alert("Internal Error: Could not find FormDef:" + formName + " Field:" + fieldName + " Id:" + id);
}
