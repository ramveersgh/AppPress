﻿
var __IsPageDirty = false;
var deletedFormDatas = new Array();
var popupDialogs = new Array(),
    $alertDialog = null,
    $promptDialog = null;
var WarnOnDirtyClose = false;
var optionsCache = new Object();

var sModalWindowFeatures = {
    dialogHeight: 450,
    dialogWidth: 650,
    status: 'no',
    resizable: 1
};

function Evaluate(obj) {
    return eval("(" + obj + ")")
}

function getKeycode(event) {
    keycode = (event.keyCode ? event.keyCode : (event.which ? event.which : event.charCode));
    //if (keycode == 27)
    //    return true;
    return false;
}

function RegexValidate(obj, regex, errorObj, errorMessage) {
    if (obj.val() != '' && obj.val() != 'Multiple' && !new RegExp(regex).test(obj.val())) {
		if (errorObj != null);
	        errorObj.html(errorMessage);
        obj.focus();
		return false;
    } else
        errorObj.html('');
return true;
}

function DateValidate(obj, dateFormat, errorObj, errorMessage) {
    errorObj.html('');
    if (obj.val() != '') {
        try {
            $.datepicker.parseDate(dateFormat, obj.val());
        } catch (e) {
			if (errorObj != null)
				errorObj.html(errorMessage);
			return false;
        }
    }
return true;
}
function MinCharsValidate(obj, MinChars, errorObj, errorMessage) {
    errorObj.html('');
    if (obj.val() != '') 
        if (obj.val().length < MinChars)
		{
			errorObj.html(errorMessage);
			return false;
			}
return true;
}
function MinimumValueValidate(obj, MinimumValue,MaximumValue, errorObj, errorMessage) {
    errorObj.html('');
    if (obj.val() != '') 
	{
        if (MinimumValue != null && parseFloat(obj.val()) < MinimumValue)
		{
			errorObj.html(errorMessage);
			return false;
			}
        if (MaximumValue != null && parseFloat(obj.val()) > MaximumValue)
		{
			errorObj.html(errorMessage);
			return false;
			}
			}
return true;
}

var FieldValidationData = new Array();

function FieldValidate(htmlId)
{
	if (FieldValidationData[htmlId])
		{
		if (FieldValidationData[htmlId].type == 'RegEx')
			return RegexValidate($(JQueryEscape('#' + htmlId)),FieldValidationData[htmlId].checkString,$(JQueryEscape('#' + FieldValidationData[htmlId].errorHtmlId)),FieldValidationData[htmlId].errorMessage);
		else if (FieldValidationData[htmlId].type == 'Date')
			return DateValidate($(JQueryEscape('#' + htmlId)),FieldValidationData[htmlId].checkString,$(JQueryEscape('#' + FieldValidationData[htmlId].errorHtmlId)),FieldValidationData[htmlId].errorMessage);
		else if (FieldValidationData[htmlId].type == 'MinChars')
			return MinCharsValidate($(JQueryEscape('#' + htmlId)),FieldValidationData[htmlId].MinChars,$(JQueryEscape('#' + FieldValidationData[htmlId].errorHtmlId)),FieldValidationData[htmlId].errorMessage);
		else if (FieldValidationData[htmlId].type == 'MinimumValue')
			return MinimumValueValidate($(JQueryEscape('#' + htmlId)),FieldValidationData[htmlId].MinimumValue,FieldValidationData[htmlId].MaximumValue,$(JQueryEscape('#' + FieldValidationData[htmlId].errorHtmlId)),FieldValidationData[htmlId].errorMessage);
		}
	return true;
}
										
function AppPressReady() {

    $(window).scrollTop();
    $(document).bind("keyup", function(event) {
        keycode = (event.keyCode ? event.keyCode : (event.which ? event.which : event.charCode));
        if (keycode == 27) {
            event.preventDefault();
        }
    });

    SetDirty(false);
    $(window).bind("beforeunload", function() {
        try {
            if (GetDirty() && GetWarnOnDirtyClose()) {
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

function _GetFieldValue(formData, fieldDefId) {
    for (var j = 0; j < formData.fieldValues.length; ++j) {
        if (formData.fieldValues[j].fieldDefId == fieldDefId)
            return formData.fieldValues[j];
    }
    return null;
}

function GetFieldValue(formData, fieldDefId) {
	var fieldValue = _GetFieldValue(formData, fieldDefId);
	if (fieldValue == null)
	    alert("Internal Error: Could not Find Field with FieldDefId:" + fieldDefId + " in FormDefId:" + formData.formDefId);
    return fieldValue;
}

function SetFieldValue(formDefId, id, fieldDefId, value) {
		var formData = FindFormData(formDefId, id);
		for (var j = 0; j < formData.fieldValues.length; ++j) {
			if (formData.fieldValues[j].fieldDefId == fieldDefId)
				{
				formData.fieldValues[j].Value=value;
				return;
				}
		}
    alert("Internal Error: Could not Find Field with FieldDefId:" + fieldDefId + " in FormDefId:" + formData.formDefId);
}

function _FindFormData(formDefId, id) {
    for (var j = 0; j < a.formDatas.length; ++j)
        if (a.formDatas[j] != null && a.formDatas[j].formDefId == formDefId && a.formDatas[j].id == id) {
            return a.formDatas[j];
        }
    return null;
}

function FindFormData(formDefId, id) {
	var formData = _FindFormData(formDefId, id);
	if (formData == null)
	    alert("Internal Error: Could not find FormData with FomDefId:" + formDefId + " Id:" + id);
    return formData;
}

function FindFormField(fieldValue) {
    for (var j = 0; j < a.formFields.length; ++j)
        if (a.formFields[j].id == fieldValue.fieldDefId)
            return a.formFields[j];
    return null;
}

function stringifyP(a) {
    return escape(JSON.stringify(a));
}

function JQueryEscape(s) {
    return s.replace(/:/g, "\\:");
}

function _AppPress_UpdatePickMultipleOptions(id) {
    var checkedItems = $('input[name="' + JQueryEscape(id) + '"]:checked');
    var s = "";
    $.each(checkedItems, function(index, checkedItem) {
        if (s.length > 0)
            s += " | ";
        s += $("label[for='" + JQueryEscape(id + ':' + checkedItem.value) + "']").text();
    });
    $(JQueryEscape('#PickMultipleSelection_' + id)).html(s);
}

function AppPress_UndoPickMultipleOptions(id, undoValue) {
    var pmItems = $('input[name="' + JQueryEscape(id) + '"]');
    $.each(pmItems, function(index, pmItem) {
		if ((","+undoValue+",").indexOf(","+pmItem.value+",") != -1)
            $(pmItem).prop('checked', true);
        else
            $(pmItem).removeAttr("checked");
    });
}

function _AppPress_GetPickMultipleValue(id) {
    var checkedItems = $('input[name="' + JQueryEscape(id) + '"]:checked');
    var s = "";
    $.each(checkedItems, function(index, checkedItem) {
        if (s.length > 0)
            s += ",";
        s += checkedItem.value;
    });
	return s;
}

function AppPress_PickMultipleDialog(id,OnChange1, popupHeight) {
	var pmValue = _AppPress_GetPickMultipleValue(id);
	$(JQueryEscape('#PickMultiplePoupup_'+id)).dialog({
		modal: true, 
		title: 'Select Options', 
		height: popupHeight,
		width: 'auto',
		buttons: [ { text: 'Ok', 
			click: function() {
				AppPress_UpdatePickMultipleOptions(id);
				var pmv = _AppPress_GetPickMultipleValue(id);
				if (pmValue != pmv)
					eval(OnChange1);
				pmValue = pmv;
				$(this).dialog('close');
				$(this).dialog('destroy');
			},
			id: 'AppPress_PickMultiple_Ok_Button'
			} ],
		beforeClose: function(event, ui) {
			AppPress_UndoPickMultipleOptions(id,pmValue);
			}, 
		close: function(event, ui) {
			$(this).dialog('close');
			$(this).dialog('destroy');
			} 
		}).keyup(function(e) {
			if (e.keyCode == $.ui.keyCode.ENTER)
				$('#AppPress_PickMultiple_Ok_Button').click();
		});
}

function AppPress_UpdatePickMultipleOptions(id) {
    _AppPress_UpdatePickMultipleOptions(id)
    //$(JQueryEscape('#PickMultiplePoupup_' + id)).dialog("close");
}

function AppPress_AddPickMultipleToPopupDialogs(id, pageStackIndex) {
    if (pageStackIndex > 0) {
        var popupDialog = popupDialogs[pageStackIndex - 1];
        popupDialog.PopupDivs[popupDialog.PopupDivs.length] = id;
    }
}

function DialogButtonOnClickFieldName(fieldName) {
    if (popupDialogs.length == 0)
        return false;
    var popupDialog = popupDialogs[popupDialogs.length - 1].popupDialog;
    for (var i = 0; i < popupDialog.dialogButtons.length; ++i)
        if (popupDialog.dialogButtons[i].fieldName == fieldName) {
            eval(popupDialog.dialogButtons[i].onClick);
            return true;
        }
    return false;
}

var AppPressRotationImages = new Array();

function AppPressRotateImage(obj)
{
	var options = AppPressRotationImages[obj.id];
	var s = obj.id.split(":");
	var formFieldType = parseInt(s[1]);
	var formDefId = s[2];
	var fieldDefId = s[3];
	var formDataId = s[4];

	var formData = FindFormData(formDefId, formDataId);
	var fieldValue = GetFieldValue(formData, fieldDefId);
	var value = fieldValue.Value;

	for (var i = 0; i < options.length; ++i)
		if (options[i].id == value)
		{
			if (i == options.length-1)
				i = 0;
			else
				i++;

			fieldValue.Value = options[i].id;
			$(JQueryEscape('#Image_'+obj.id)).addClass(options[i].value);
			break;
		}
}

function AlertMessage(message, width) {
    if (!width)
        width = 450;
    $alertDialog = $('<div></div>')
        .html(message)
        .dialog({
            modal: true,
            closeText: null,
            width: width,
            title: "Alert",
            close: function(event, ui) {
                if ($alertDialog != null) {
                    $alertDialog.dialog('destroy').remove();
                    $alertDialog = null;
                }
            }
        });
}

function DialogButtonOnClick(ev) {
    if (!ev) ev = window.ev;
    DialogButtonOnClickFieldName($(ev.target).text());
}

function _ExecuteAppPressResponse(pageData,instanceId, clientActions, startIndex) {
    for (var cai = startIndex; cai < clientActions.length; ++cai) {
        var ca = clientActions[cai];
        switch (parseInt(ca.appPressResponseType)) {
			case 27: // ChangeFormDataId
				// Form Error
				var prefix = "#_FormError:" + ca.formDefId + ':';
				$(JQueryEscape(prefix + ca.id)).prop("id",prefix+ca.Value);
				// Field Id
				var prefix = "AppPress:" + ca.message + ':' + ca.formDefId + ':' + ca.fieldDefId + ':';
				// Radio Id has option id as suffix
				var items = $('[id^="'+JQueryEscape(prefix + ca.id)+'"]');
				for (var i = 0; i < items.length; ++i)
					{
					var item = items[i];
					var id = item.id.replace(prefix+ca.id,'');
					id = prefix+ca.Value+id;
					$(item).prop("id",id);
					}
				// name for radio
				$( "[name='"+JQueryEscape(prefix + ca.id)+"']" ).prop("name",prefix+ca.Value);
				// for label
				$( "label[for='"+JQueryEscape(prefix + ca.id)+"']" ).prop("for",prefix+ca.Value);
				$(JQueryEscape('#'+prefix + ca.id)).prop("id",prefix+ca.Value);
				// Field Container
				var prefix = "fieldContainer:" + ca.formDefId + ':' + ca.fieldDefId + ':';
				$(JQueryEscape('#'+prefix + ca.id)).prop("id",prefix+ca.Value);
				// Error Field
				var prefix = "error:" + ca.formDefId + ':' + ca.fieldDefId + ':';
				$(JQueryEscape('#'+prefix + ca.id)).prop("id",prefix+ca.Value);
				break;
            case 10 : // ExecuteJSScript
                if (ca.JsStr)
                    try {
                        eval(ca.JsStr);
                    } catch (e) {} // ignore errors
                break;
            case 20 : // PromptClient
                $promptDialog = $('<div></div>')
                    .html(ca.message)
                    .dialog({
                        autoOpen: false,
                        modal: true,
                        closeText: null,
                        width: ca.popupWidth == 0 ? 450 : ca.popupWidth,
                        title: ca.popupTitle == null ? "Confirm" : ca.popupTitle,
                        close: function(event, ui) {
                            $(this).dialog('destroy').remove();
                            $promptDialog = null;
                        },
                        buttons: {
                            "Ok": function() {
                                result = true;
                                $(this).dialog("close");
								var clickObj = new Object();
								clickObj.id = "AppPress:99:"+ca.formDefId+":"+ca.fieldDefId+":"+ca.id;
                                AjaxFunctionCall(pageData.functionCall, instanceId, ca.fieldDefId, false, clickObj, result)
                            },
                            "Cancel": function() {
                                $(this).dialog("close");
                            }
                        }
                    });

                $promptDialog.dialog('open');
                break;
            case 17 : //FormError
                var message = ca.message;
                var obj = $(JQueryEscape("#_FormError:" + ca.formDefId + ':' + ca.id));
                if (obj.length == 0) {
                    message = " Error in Form:" + ca.formDefId + " with Id: " + ca.id + "\n" + $('<div/>').html(message).text();
                    AlertMessage(message);
                    break;
                } else {
                    var containerObj = $(JQueryEscape("#_FormErrorContainer:" + ca.formDefId + ':' + ca.id));
                    if (containerObj.length > 0) {
                        containerObj.toggle(true);
                    }
                }
                obj.html(message);
                break;
            case 19 : //SetPageNonDirty
                SetDirty(false);
                break;
            case 1 : //AlertMessage
                $alertDialog = $('<div></div>')
                    .html(ca.message)
                    .dialog({
                        modal: true,
                        closeText: null,
                        width: ca.popupWidth == 0 ? 450 : ca.popupWidth,
                        title: ca.popupTitle == null ? "Alert" : ca.popupTitle,
                        close: function(event, ui) {
                            if ($alertDialog != null) {
                                $alertDialog.dialog('destroy').remove();
                                $alertDialog = null;
                            }
                            _ExecuteAppPressResponse(pageData,instanceId, clientActions, cai + 1);
                        }
                    });

                return;
            case 9 : //PageRefresh
                __IsPageDirty = false;
				disablePageCount = 0;
                location.reload(true);
                break;
            case 3 : //ClearErrors
                if (ca.fieldDefId) {
                    var id = 'error:' + ca.formDefId + ':' + ca.fieldDefId + ':' + ca.id;
                    var obj = $('#' + JQueryEscape(id));
                    if (obj.length == 1)
                        obj.html('');
                } else {
                    $("[id^='" + JQueryEscape("error:") + "']").html('');
                    try {
                        $("#_FormErrorContainer").toggle(false);
                    } catch (e) {}
                    $("#_FormError").html('');
                }
                break;
            case 4 : // Popup
                var $popupDialog = $("<div></div>");
                var popupDialog = new Object();
                popupDialog.popupDialog = $popupDialog;
                popupDialog.rootFormName = ca.formDefId;
                popupDialog.IsPageDirty = false;
                popupDialog.WarnOnDirtyClose = false;
                popupDialog.pageTimeStamp = ca.pageTimeStamp;
                popupDialog.PopupDivs = new Array();
                popupDialogs.push(popupDialog);
				var id = 'fieldContainer:' + ca.formDefId + ':0:' + ca.id;
				
				// no focus
				if (ca.NoFocus)
					$.ui.dialog.prototype._focusTabbable = $.noop;

                $popupDialog.html("<div id='"+id+"'>"+ca.fieldHtml+"</div>");
				var popupWidth = ca.popupWidth;
				if (popupWidth > $(document).width())
					popupWidth = $(document).width();
                $popupDialog.dialog({
                    autoOpen: false,
                    modal: true,
                    closeText: null,
                    width: popupWidth,
                    height: ca.popupHeight,
					position: { my: ca.popupPosition, at: ca.popupPosition, of: window },
                    //maxHeight: 600, //Ram: Do not se max height because it becume cause of scroll and also not allow resize height. Height can be passed manually if it is fixed.
                    title: ca.popupTitle,
                    open: function(evt, ui) {
                        if (ca.NoFocus)
                            $(this).parent().focus();; // set focus to dialog so Escape works
                        // ??? check if focus on Date then Blur
						        if ($.ui && $.ui.dialog && $.ui.dialog.prototype._allowInteraction) {
            var ui_dialog_interaction = $.ui.dialog.prototype._allowInteraction;
            $.ui.dialog.prototype._allowInteraction = function (e) {
                if ($(e.target).closest('.select2-dropdown').length) return true;
                return ui_dialog_interaction.apply(this, arguments);
            };
        }
                    },
                    close: function(event, ui) {
                        var popupDialog = popupDialogs[popupDialogs.length - 1];
                        popupDialog.popupDialog.dialog('destroy').remove();
                        for (var i = 0; i < popupDialog.PopupDivs.length; ++i)
                            $(JQueryEscape('#PickMultiplePoupup_' + popupDialog.PopupDivs[i])).dialog('destroy').remove();
                        popupDialogs.pop();
                        a.pageStackCount--;
                        // remove FormDatas of popup
                        for (var i = 0; i < a.formDatas.length; ++i)
                            if (a.formDatas[i].pageStackIndex > a.pageStackCount)
                                a.formDatas[i] = null;
                    },
                    beforeClose: function(event, ui) {
                        var popupDialog = popupDialogs[popupDialogs.length - 1];
                        if (popupDialog.IsPageDirty) {
                            if (popupDialog.WarnOnDirtyClose)
                                if (!window.confirm("This dialog is unsaved. Do you really want to close it?"))
                                    return false;
                        }
                    }
                });
                if (ca.popupParams != null && ca.popupParams.dialogButtons != null) {
                    var dialogButtons = {};
                    for (var i = 0; i < ca.popupParams.dialogButtons.length; ++i) {
                        dialogButtons[ca.popupParams.dialogButtons[i].fieldName] = DialogButtonOnClick;
                    }
                    $popupDialog.dialogButtons = ca.popupParams.dialogButtons;
                    $popupDialog.dialog('option', 'buttons', dialogButtons);
                }
                $popupDialog.dialog('open');
                if (ca.JsStr)
                    eval(ca.JsStr);

                SetFieldReadonly(pageData);
				if (typeof(AppPressRefresh)=='function')
					AppPressRefresh();
                popupDialog.p = pageData;

                //return; need to show fieldError from Init
                break;
            case 13 : // ClosePopupWindow
                if (popupDialogs.length > 0) {
                    var popupDialog = popupDialogs[popupDialogs.length - 1];
                    popupDialog.popupDialog.dialog('destroy').remove();
                    for (var i = 0; i < popupDialog.PopupDivs.length; ++i)
                        $(JQueryEscape('#PickMultiplePoupup_' + popupDialog.PopupDivs[i])).dialog('destroy').remove();
                    popupDialogs.pop();
                }
                break;
            case 7 : // Redirect
                if (GetDirty() && GetWarnOnDirtyClose()) {
                    if (!window.confirm('You have some un-saved data in this page, do you want to continue with out saving those data?'))
                        continue;
                    __IsPageDirty = false; // do not repeat the warning in page unload
                }
                DisablePage();

                if (ca.url.indexOf("GetPDF=") > 0) {
                    window.setTimeout(function() {
                        RemoveDisablePage();
                    }, 8000);
                }

                if (ca.redirectParams == null)
                    try {
                        RemoveDisablePage(); // does not finish AjaxFunctionCall. So DiablePageCount remains 1
                        location.href = ca.url;
                    } catch (e) {
                    // jQuery has bug in this case for window closing event
                } else {

                    var formPost = '';
                    var formStr = '<form action="' + ca.url + '" method="post"';
                    $.each(ca.redirectParams.postParams, function(key, value) {
                        formPost += '<input type="hidden" name="' + key + '" value="' + value + '">';
                    });
					if (ca.redirectParams.alertMessage != null) {
                        formPost += '<input type="hidden" name="_AppPressAlertMessage" value="' + ca.redirectParams.alertMessage + '">';
					}
                    if (ca.redirectParams.target != null) {
                        if (ca.redirectParams.target != "_self")
                            RemoveDisablePage();
                        formStr += ' target="+ca.redirectParams.target+"';
                    }
                    formStr += '>' + formPost + '</form>';
                    $(formStr).appendTo('body').submit();
                }

                break;
            case 11 : //SetFieldValue
                var formData = FindFormData(ca.formDefId, ca.id);
                var fieldValue = GetFieldValue(formData, ca.fieldDefId);
				var formField = FindFormField(fieldValue);
                var fieldType = formField.Type;
                switch (fieldType) {
                    case 2 :
                    case 1 :
                    case 3 :
                    case 4 :
                    case 11 :
                        var id = "AppPress:" + fieldType + ':' + ca.formDefId + ':' + ca.fieldDefId + ':' + ca.id;
                        var obj = $("#" + JQueryEscape(id));
                        obj.val(ca.Value);
                        break;
                    case 6 :
                        var style = formField.Style;
                        if (style == 5 )
                            $("[Name='" + JQueryEscape(objId) + "']:checked").val(ca.Value);
                        else if (style == 9 ) {
                            var obj = $(JQueryEscape("#AppPress:" + fieldType + ":" + ca.formDefId + ':' + ca.fieldDefId + ':' + ca.id));
                            obj.val(ca.fieldHtml);
                            fieldValue.Value = ca.Value;
                        } else { // FormDefFieldStyle.DropDown
                            var obj = $(JQueryEscape("#AppPress:" + fieldType + ":" + ca.formDefId + ':' + ca.fieldDefId + ':' + ca.id));
                            obj.val(ca.Value);
                        }
                        SetDirty();
                        break;
                    case 14 :
                        var obj = $(JQueryEscape("#fieldContainer:" + formData.formDefId + ':' + ca.fieldDefId + ':' + ca.id));
                        obj.html(ca.fieldHtml);
                        break;
                    case 5 :
                        if (ca.id == null)
                            $('input|checkbox[id*="' + JQueryEscape(ca.formDefId + ':' + ca.fieldDefId + ':"]')).attr('checked', ca.Value == '1');
                        else {
                            var obj = $(JQueryEscape("#AppPress:" + fieldType + ":" + formData.formDefId + ':' + ca.fieldDefId + ':' + ca.id));
                            if (ca.Value == "1")
                                obj.attr("checked", "checked");
                            else
                                obj.removeAttr("checked");
                        }
                        break;
                }
                break;
            case 14 : //SetFocus
                var formData = _FindFormData(ca.formDefId, ca.id);
				if (formData != null)
					{
					var fieldValue = _GetFieldValue(formData, ca.fieldDefId);
					if (fieldValue != null)
						{
						var objSetFocus = "#AppPress:" + FindFormField(fieldValue).Type.toString() + ":" + ca.formDefId + ':' + ca.fieldDefId + ':' + ca.id;
						setTimeout(
							function() {
								try {
									$(JQueryEscape(objSetFocus)).focus().select();
								} catch (e) {
									// ignore errors
								}
							});
						}
					}
                break;
            case 2 : //FieldError
                var id = 'error:' + ca.formDefId + ':' + ca.fieldDefId + ':' + ca.id;
                var obj = $('#' + JQueryEscape(id));
                if (obj.length == 0) {
					if (ca.message != null)
						AlertMessage(ca.message+" <br/>Could not find Error Tag for " + ca.fieldHtml);
                } else
                    obj.html(ca.message==null?"":ca.message);
                break;
            case 18 : //FieldHelp
                var id = 'help:' + ca.formDefId + ':' + ca.fieldDefId + ':' + ca.id;
                var obj = $('#' + JQueryEscape(id));
                if (obj.length == 0) {
					if (ca.message != null)
						AlertMessage(ca.message+" <br/>Could not find Help Tag for " + ca.fieldHtml);
                } else
                    obj.html(ca.message==null?"":ca.message);
                break;

            case 5 : //RefreshField
                var id = 'fieldContainer:' + ca.formDefId + ':' + ca.fieldDefId + ':' + ca.id;
                var obj = $('#' + JQueryEscape(id));
                if (obj.length == 0) {
                    alert("Internal Error: Could not find " + id+"\n"+ca.message);
                } else {
                    if (ca.outer)
                        obj[0].outerHTML = ca.fieldHtml;
                    else
                        obj.html(ca.fieldHtml);
						// recheck containerId
					obj = $('#' + JQueryEscape(id));
					if (obj.length == 0) 
						alert("Internal Error: Refresh field is removing field AppPressContainerId");
                    if (ca.JsStr)
                        eval(ca.JsStr);
                    SetFieldReadonly(pageData);
                }
				// For any client side updates
				if (typeof(AppPressRefresh)=='function')
					AppPressRefresh();
                break;
            case 22 : //SetPageDirty
                SetDirty(ca.Value == "1" ? true : false);
                break;
            case 23 : //OpenUrl
                jQuery('<form action="' + ca.url + '" method="post" target="' + ca.id + '">' + ca.message + '</form>')
                    .appendTo('body').submit().remove();

                break;
            case 16 : //DownloadFile
                jQuery('<form action="' + ca.url + '" method="post"><input type="hidden" name="' + ca.message + '" value="' + ca.Value + '"></form>')
                    .appendTo('body').submit().remove();

                break;
			case 24 : //RemoteRefresh
                AjaxFunctionCall("RefreshField",ca.instanceId,ca.fieldDefId, false, obj);
                break;
            default:
                alert("Internal Error: Could not find AppPressResponseType:" + ca.appPressResponseType);
                break;
        }
    }
}

function ExecuteAppPressResponse(a,instanceId) {
    if (a.appPressResponse != null) {
        var appPressResponse = a.appPressResponse;
        a.appPressResponse = null;
        _ExecuteAppPressResponse(a,instanceId, appPressResponse, 0);
        a.appPressResponse = null;
    }
}

function UnSelectRestSelectRow(obj) {
    var s = obj.id.split(":");
    if (s.length < 5) {
        alert("Invalid Object Id:" + obj.id);
    }
    var formFieldType = s[1];
    var formName = s[2];
    var fieldName = s[3];
    var formId = s[4];
	var selectAll = $(JQueryEscape('[id^=SelectAll_AppPress:'+formFieldType+':'+formName+':'+fieldName+':]'));
	if (selectAll.length==0 || selectAll.prop('disabled')) // Select All Visible
	{
		$(JQueryEscape("input[id^='AppPress:" + formFieldType + ":" + formName + ":" + fieldName + ":']")).each(function() { //loop through each checkbox
			if (this.id != obj.id)
				this.checked = false; //select all checkboxes with class "checkbox1"               
		});
		if (typeof(AppPressRefresh)=='function')
			AppPressRefresh();
	}
}

function OnChange(obj, instanceId, always) {
   
    a.RefreshFields = new Array();
    $.each(a.DependentFields,
        function(index, value) {
            $.each(value, function(index1, value1) {
                if (value1 == obj.name || value1 == obj.id || 'PickMultiplePoupup_'+value1 == obj.id) // for Radios Name contains field id
					{
                    a.RefreshFields.push(index+","+value1);
					}
            })
        })

    if (always || a.RefreshFields.length > 0) {
        var s = obj.id.split(":");
        if (s.length < 5) {
            alert("Invalid Object Id:" + obj.id);
        }
        var formFieldType = s[1];
        var formName = s[2];
        var fieldName = s[3];
        var formId = s[4];
        AjaxFunctionCall("OnChange", instanceId, fieldName, false, obj);
    }

}

function DeleteFile(objId, instanceId) {
    var obj = document.getElementById(objId);
    a.RefreshFields = new Array();
    $.each(a.DependentFields,
        function(index, value) {
            $.each(value, function(index1, value1) {
                if (value1 == obj.name || value1 == obj.id) // for Radios Name contains field id
                    a.RefreshFields.push(index)
            })
        })


    var s = obj.id.split(":");
    if (s.length < 5) {
        alert("Invalid Object Id:" + obj.id);
    }
    var formFieldType = s[1];
    var formName = s[2];
    var fieldName = s[3];
    var formId = s[4];
    AjaxFunctionCall("DeleteFile", instanceId, fieldName, false, obj);
}

function GetFormData(a, clickEvent, checkAutoComplete) {
    // Scan all fields and Create a new FormData list
    var evn = null;
    if (clickEvent)
        evn = clickEvent;
    else if (this.event && this.event.type == "click")
        evn = this.event;
    $.each(a.formDatas, function(index, formData) {
        if (formData != null)
            $.each(formData.fieldValues, function(index, fieldValue) {
                var formField = FindFormField(fieldValue);
                if (formField != null)
                    switch (formField.Type) {
                        case 7 :
                            if (!formField.Static)
                                fieldValue.Value = null;
                            break;
                    }
            })
    })
    var objs = $("[id^='" + JQueryEscape("AppPress:") + "']").filter(':input');
    for (var ii = 0; ii < objs.length; ++ii) {
        var obj = $(objs[ii]);
		if (!FieldValidate(obj[0].id))
			return false;
        var s = obj[0].id.split(":");
        if (s.length < 5) {
            alert("Invalid Object Id:" + obj[0].id);
        }
        for (var jj = 0; jj < objs.length; ++jj)
            if (jj != ii && objs[jj].id == obj[0].id)
                alert("Found 2 objects with Id: " + obj[0].id);
        var formFieldType = parseInt(s[1]);
        var formDefName = s[2];
        var fieldName = s[3];
        var formDataId = s[4];
        switch (formFieldType) {
            case 2 :
				{
				   var formData = FindFormData(formDefName, formDataId);
                    var fieldValue = GetFieldValue(formData, fieldName);
					var style = FindFormField(fieldValue).Style;
					if (style == 18 || style == 17 || style == 16 ) { 
						fieldValue.Value = CKEDITOR.instances[obj[0].id].getData();
						break;
					}
				}
            case 1 :
            case 3 :
            case 4 :
            case 8 :
                {
                    var formData = FindFormData(formDefName, formDataId);
                    var fieldValue = GetFieldValue(formData, fieldName);
					if (fieldValue.ReadOnly != 1)
						fieldValue.Value = obj.val() == '' ? null : obj.val();
                    break;
                }
            case 5 :
                {
                    var formData = FindFormData(formDefName, formDataId);
                    var fieldValue = GetFieldValue(formData, fieldName);
					if (fieldValue.ReadOnly != 1)
	                    fieldValue.Value = obj.prop("checked") ? "1" : "0";
                    break;
                }
            case 6 :
                {
                    var formData = FindFormData(formDefName, formDataId);
                    var fieldValue = GetFieldValue(formData, fieldName);
					if (fieldValue.ReadOnly != 1)
					{
						var style = FindFormField(fieldValue).Style;
						if (style == 5 ) {
							if (obj.prop("checked"))
								fieldValue.Value = obj.val();
						} else if (style == 9 ) {
								fieldValue.Value = obj.val();
							if (fieldValue.Value == "")
								fieldValue.Value = null;
						} else {
							fieldValue.Value = obj.val();
							if (fieldValue.Value == "")
								fieldValue.Value = null;
						}
					}
                    break;
                }
            case 7 :
                {
                    var formData = FindFormData(formDefName, formDataId);
                    var fieldValue = GetFieldValue(formData, fieldName);
					if (fieldValue.ReadOnly != 1)
					{
						var style = FindFormField(fieldValue).Style;
						if (style == 11 ) {
							if (obj.prop("checked"))
								if (fieldValue.Value == null)
									fieldValue.Value = obj.val();
								else
									fieldValue.Value += "," + obj.val();
						}
						else if (style == 6 ) {
							if (obj.val() != null)
								fieldValue.Value = obj.val().join();
						}
					}
                    break;
                }
            case 11 :
                {
                    var formData = FindFormData(formDefName, formDataId);
                    var fieldValue = GetFieldValue(formData, fieldName);
                    fieldValue.Value = obj.attr("FileId");
                    if (typeof(fieldValue.Value) == "undefined" || fieldValue.Value == '')
                        fieldValue.Value = null;
                    break;
                }
        }
    }
    return true;
}

var clickEvent = null;

function CatchEvent(event) {
    clickEvent = event;
}
var disablePageCount = 0;

function DisablePage() {
    if (disablePageCount == 0) {
        if ($("#overlay").length == 0)
            $("body").append('<div id="overlay" style="opacity: 0.5;background-color:grey;position:fixed;top:0;left:0;bottom:0px;right:0px;z-index:10000"></div>');
        else
            $("#overlay").show();
    }
    disablePageCount++;
}

function RemoveDisablePage() {
    disablePageCount--;
    if (disablePageCount == 0) {
        if ($("#overlay").length > 0)
            $("#overlay").hide();
    }
}

function HandleShortcut(obj) {
    if ($promptDialog == null && $alertDialog == null) {
        obj.click();
    }
}

function HandleFocusShortcut(obj) {
    if ($promptDialog == null && $alertDialog == null && obj.is(":focus")) {
        obj.change();
    }
}

var isAjaxCallRunning = false;

function AjaxFunctionCall(functionName, instanceId, fieldName, NoSubmit, sender, result) {
    if (sender && sender.attributes && sender.attributes['disabled'] && sender.attributes['disabled'].value == 'disabled') // could not do this with jQuery
        return false; // for disabling links
    if (isAjaxCallRunning) {
        var interval = setInterval(
            function() {
                if (!isAjaxCallRunning) {
                    clearInterval(interval);
                    AjaxFunctionCall(functionName, instanceId, fieldName, NoSubmit, sender, result);
                }
            },
            250);
    } else {
		if (!FieldValidate(sender.id))
			return;

			        var disablePageTimer = null;
        if (functionName == 'OnChange') {
            disablePageTimer = setTimeout(function() {
                disablePageTimer = null;
                DisablePage();
            }, 2500)
        } else
            DisablePage();


		var s = sender.id.split(":");
		if (s.length < 5)
			{
			alert("Internal Error: Field id: "+s+" not correct for Click Or Change");
			return;
			}
		var formId = s[4];
        isAjaxCallRunning = true;
        a.screenWidth = screen.width;
        a.screenHeight = screen.height;
        a.documentWidth = $(document).width();
        a.documentHeight = $(document).height();
        a.windowWidth = $(window).width();
        a.windowHeight = $(window).height();
        if (NoSubmit)
            a.FormDatas = null;
        else
        // get clientChanges
        if (!GetFormData(a, clickEvent, true)) {
            isAjaxCallRunning = false;
            return;
        }
        a.appPressResponse = null;
        a.rootFormData = new Object();
        a.rootFormData.formDefId = rootFormName;
        a.rootFormData.id = rootFormId;
        a.fieldValue = new Object();
        a.fieldValue.fieldDefId = fieldName;
        a.formDataId = formId;
        a.PromptClientResult = result;
        a.pageTimeStamp = pageTimeStamp;
        a.PageDirty = GetDirty();
		a.autoCompleteTerm = null;
        var pageurl = unescape(GetBaseUrl(instanceId));
        sender = $(sender);

        var formFields = a.formFields;
        a.formFields = null; // do not post it back

		a.instanceId = parseInt(instanceId);
        
		var s = stringifyP(a);
		
        a.formFields = formFields;
	    pageurl += "?functionCall=" + functionName;
        $.ajax({
            type: 'POST',
            url: pageurl,
            data: {
                'p': s
            },
            success: function(result) {
                if (result != null && result != "") // comes in case Serialize throws a error
                {
                    var p1;
                    try {
                        p1 = eval('(' + result + ')');
                    } catch (ex) {
                        RemoveDisablePage();
                        isAjaxCallRunning = false;
                        AlertMessage(result, 700);
                        return;
                    }
                    if (p1.formDatas == null)
                    // error case, show the error without disturbing p
                        ExecuteAppPressResponse(p1,instanceId);
                    else {
                        a = p1;
                        ExecuteAppPressResponse(a,instanceId);
                    }
                }
                if (functionName == 'OnChange')
                    if (disablePageTimer != null)
                        clearTimeout(disablePageTimer);
                    else
                        RemoveDisablePage();
                else
                    RemoveDisablePage();
                isAjaxCallRunning = false;
            },
            error: function(obj, errorStr, errorThrown) {
                AlertMessage('Server Not Responding. Please retry.');
                if (functionName == 'OnChange')
                    if (disablePageTimer != null)
                        clearTimeout(disablePageTimer);
                    else
                        RemoveDisablePage();
                else
                    RemoveDisablePage();
                isAjaxCallRunning = false;
            },
            complete: function(obj, status) {
                //if (!PageBeingRefreshed)
                //	RemoveDisablePage();
                isAjaxCallRunning = false;
            }
        });
    }
}

function AutoCompleteCall(instanceId, formId, fieldName, term, callback) {

	var data = {
		results: []
	};
	if (term == null || term == "")
	{
		callback(data);
		return;
		}
    a.screenWidth = screen.width;
    a.screenHeight = screen.height;
    a.documentWidth = $(document).width();
    a.documentHeight = $(document).height();
    a.windowWidth = $(window).width();
    a.windowHeight = $(window).height();
    // get clientChanges
    GetFormData(a, clickEvent, false);
    a.appPressResponse = null;
    a.rootFormData = new Object();
    a.rootFormData.formDefId = rootFormName;
    a.rootFormData.id = rootFormId;
    a.fieldValue = new Object();
    a.fieldValue.fieldDefId = fieldName;
    a.formDataId = formId;
	a.instanceId = parseInt(instanceId);
    a.autoCompleteTerm = term;
    var s = stringifyP(a);
    var pageurl = unescape(GetBaseUrl(instanceId));
    pageurl += "?functionCall=AutoCompleteOptions";
    $.ajax({
        type: 'POST',
        url: pageurl,
        data: {
            'p': s
        },
        success: function(result) {
            var responseData = eval('(' + result + ')');
            ExecuteAppPressResponse(responseData,instanceId);

			for (var v = 0; v < responseData.autoCompleteOptions.length; v++) {
			  data.results.push({
				id: responseData.autoCompleteOptions[v].id,
				text: responseData.autoCompleteOptions[v].value
			  });
			}
            callback(data);
        },
        error: function(obj, errorStr, errorThrown) {
            AlertMessage('Server Not Responding. Please retry.');
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

function CheckUncheckAll(obj, id) {
	var idParts = id.split(":");
	var idPrefix = idParts[0]+":"+idParts[1]+":"+idParts[2]+":"+idParts[3]+":";
    $("[type='checkbox'][id^='" + JQueryEscape(idPrefix) + "']").prop("checked", $(obj).prop("checked"));
	if (typeof(AppPressRefresh)=='function')
		AppPressRefresh();
}

function GetDirty() {
    return popupDialogs.length == 0 ? __IsPageDirty : popupDialogs[popupDialogs.length - 1].IsPageDirty;
}

function GetWarnOnDirtyClose() {
    return popupDialogs.length == 0 ? WarnOnDirtyClose : popupDialogs[popupDialogs.length - 1].WarnOnDirtyClose;
}

function SetWarnOnDirtyClose() {
    if (popupDialogs.length == 0)
        WarnOnDirtyClose = true;
    else
        popupDialogs[popupDialogs.length - 1].WarnOnDirtyClose = true;
}


function SetFieldReadonly(a) {
    $.each(a.formDatas, function(index, formData) {
        $.each(formData.fieldValues, function(index, fieldValue) {
                var formField = FindFormField(fieldValue)
                if (formField != null)
                    switch (formField.Type) {
                        case 7 :
                        case 6 :
                        case 4 :
                        case 1 :
                        case 2 :
                        case 8 :
                        case 11 :
                        case 12 :
                        case 5 :
                            {
                                var dirty = null;
                                switch (fieldValue.ReadOnly) {
                                    case 1 :
                                        dirty = "disabled";
                                        break;
                                }
                                if (dirty != null)
									if (FindFormField(fieldValue).Style == 5 ) {} else {
										var id = "AppPress:" + FindFormField(fieldValue).Type + ":" + formData.formDefId + ':' + fieldValue.fieldDefId + ':' + formData.id;
											$("[id^='" + JQueryEscape(id)+"']").attr('disabled', dirty);
										//else
										//    $('#' + JQueryEscape(id)).removeAttr('disabled');
									}

                            }
                            break;
                    }
            }

        )
    })
}

function SetDirty(dirty) {
    if (typeof(dirty) == "undefined")
        dirty = true;
    var pageDirty = GetDirty();
    if (pageDirty != dirty) {
        if (popupDialogs.length == 0) {
            __IsPageDirty = dirty;
            SetFieldReadonly(a);
        } else {
            popupDialogs[popupDialogs.length - 1].IsPageDirty = dirty;
            SetFieldReadonly(popupDialogs[popupDialogs.length - 1].p);
        }
    }
}

function AppPressAutoComplete(id, onChange, instanceId) {
    var s = id.split(":");
    if (s.length < 5) {
        alert("Invalid Object Id:" + obj.id);
    }
    var formFieldType = s[1];
    var formDefId = s[2];
    var fieldDefId = s[3];
    var formId = s[4];
    $('#' + JQueryEscape(id)).autocomplete({
            source: function(req, add) {
                AutoCompleteCall(instanceId, formId, fieldDefId, req.term, add)
            },
            select: function(event, ui) {
                eval('var formData = FindFormData(formDefId,formId);var fieldValue = GetFieldValue(formData, fieldDefId);fieldValue.Value = ui.item.id;' +
                    onChange);
            },
            open: function() {
                $('#' + JQueryEscape(id)).bind('blur', function() {
                    if (typeof $(this).data('uiItem') == 'undefined') {
                        $(this).val(null);
                    }
                });
            },
            close: function() {
                $('#' + JQueryEscape(id)).unbind('blur');
            }
        })
        .focusout(function() {
            if (!$(this).val()) {
                eval('var formData = FindFormData(formDefId,formId);var fieldValue = GetFieldValue(formData, fieldDefId);fieldValue.Value = null;' +
                    onChange);
            }
        })
        .bind('paste', function(e) {
            setTimeout(function() {
                $('#' + JQueryEscape(id)).autocomplete('search', $('#' + JQueryEscape(id)).val());
            }, 0);
        });
}

function FileUploadUI(objId,serverUrl,Accept,AutoUpload,LocalInstanceId, SaveOnUpload)
{
    $(JQueryEscape('#' + objId)).attr('data-url',serverUrl);
    if (Accept != '')
        $(JQueryEscape('#' + objId)).attr('accept',Accept);
try {
$(JQueryEscape('#' + objId)).fileupload({
    dataType: 'text',
    maxNumberOfFiles:1,
    add: function (e, data) {
        if (data.files.length > 0)
        {
            $(JQueryEscape('#UIProgress:' + objId + '.bar')).css('width',0);
            $(JQueryEscape('#UIFileName:' + objId)).html(data.files[0].name).removeClass().addClass('fileUploadPendingFileName');
            $(JQueryEscape('#UIProgress:' + objId)).addClass('fileUploadProgress');
			
			
                if (AutoUpload)
				{
                    $(JQueryEscape('#UIFileUpload:' + objId)).off('click').attr('itemprop','Cross').click(function () {
                                    data.abort();
                                    $(JQueryEscape('#UIProgress:' + objId)).removeClass();
                                    $(JQueryEscape('#UIFileUpload:' +objId)).attr('itemprop','None');
                                });
                                data.submit();
				}
				else
				{
			                $(JQueryEscape('#UIFileUpload:' + objId)).off('click').attr('itemprop','Upload');
                                    $(JQueryEscape('#UIFileUpload:' + objId)).click(function () {
                                        $(JQueryEscape('#UIFileUpload:' + objId)).off('click').attr('itemprop','Cross').click(function () {
                                            data.abort();
                                            $(JQueryEscape('#UIProgress:' + objId)).removeClass();
                                            $(JQueryEscape('#UIFileUpload:' + objId)).attr('itemprop','None');
                                        });
                                        data.submit();
                                    });
				}

        }
    },
    done: function (e, textData) {
        try {
            var data = eval('(' + textData.result + ')');
            if (data.Error) {
                    AlertMessage(data.Error);
                    $(JQueryEscape('#UIProgress:' + objId + '.bar')).css('width',0);
                    $(JQueryEscape('#UIProgress:' + objId)).removeClass();
                    $(JQueryEscape('#UIFileUpload:' + objId)).attr('itemprop','None');
            }
            else {
                $('#'+JQueryEscape(objId)).attr('FileId',data[0].Id)
                $(JQueryEscape('#UIFileName:' +objId)).removeClass().addClass('fileUploadUploadedFileName');
                $(JQueryEscape('#UIProgress:' + objId)).removeClass();
                $(JQueryEscape('#UIFileUpload:' + objId)).attr('itemprop','None');
                OnChange($(JQueryEscape('#' + objId))[0],LocalInstanceId,true);
				if (!SaveOnUpload)
					SetDirty(true);
            }
        } catch (ex) {
             AlertMessage(textData.result,700);
            $(JQueryEscape('#UIProgress:' + objId + '.bar')).css('width',0);
            $(JQueryEscape('#UIProgress:' + objId)).removeClass();
            $(JQueryEscape('#UIFileUpload:' + objId)).attr('itemprop','None');
        }
    },
    error: function (e, data) {
        $(JQueryEscape('#UIProgress:' +objId)).removeClass();
        $(JQueryEscape('#UIFileUpload:' + objId)).attr('itemprop','None');
       if (e.responseText)
        AlertMessage(e.responseText,700);
    },
    progressall: function (e, data) {
        var progress = parseInt(data.loaded / data.total * 100, 10);
        $(JQueryEscape('#UIProgress:' + objId + '.bar')).css(
            'width',
            progress + '%'
        );
    }
});
}
catch(e) {
}
}
function FillOptionsFromCache(optionId, htmlId, language, ignoreNull)
{
	var html = '';
	var options = optionsCache[optionId];
	for (var i = 0; i < options.length; ++i)
		{
		if (ignoreNull && options[i]['id'] == '')
			continue;
		var v = options[i][language];
		if (v)
			v = options[i].localizedValues[language];
		else
			v = options[i].localizedValues['English'];
		html += '<option value="' +options[i]['id'] + '">' + v + '</option>';
		}             
	var obj = $(JQueryEscape('#'+htmlId));   
	//if (obj.length == 0)
	//	alert("Internal Error Could not find Object with Id: "+htmlId);   
	obj.html(html);
}

function toTitleCase(str)
{
    return str.replace(/\w\S*/g, function(txt){return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase();});
}

function toUpperCase(str)
{
    return str.toUpperCase();
}

function toLowerCase(str)
{
    return str.toLowerCase();
}

var RowFilterDiv = null;

function AppPress_GridFilterClick(cid,oid, undo)
{
	var o = $(JQueryEscape('#'+cid));
	var b = $(JQueryEscape('#FilterButton_'+oid));
	if (o.is(":visible"))
		{
		if (undo)
			{
			$(JQueryEscape('[name='+oid+']')).each ( function () 
				{
				this.checked = this.originalChecked;
				} );
			}
		o.hide();
		RemoveDisablePage();
		b.css('z-index',"");
		o.css('z-index',"");
		RowFilterDiv = null;
		$(document).off('keyup',RowFilterKeyup);
		}
	else
		{
		DisablePage();
		o.css('z-index',"10001");
		o.show();
		o.focus();
		o.css('top',b.position().top+b.outerHeight(true));
		o.css('left',b.position().left);
		$(JQueryEscape('[name='+oid+']')).each ( function () 
			{
			this.originalChecked = $(this).prop('checked');
			} );
		$(JQueryEscape('#SelectAll_'+oid)).prop('checked',false);
		if (typeof(AppPressRefresh)=='function')
			AppPressRefresh();
		RowFilterDiv = o;
		$(document).on('keyup',RowFilterKeyup);
		}
}

function RowFilterKeyup(e) {
	if (RowFilterDiv != null)
	{
	if (e.keyCode == $.ui.keyCode.ENTER)
		{
		RowFilterDiv.find("[name='AppPress_RowFilter_Apply']").click();
		e.stopPropogation();
		}
	else if (e.keyCode == $.ui.keyCode.ESCAPE)
		{
		RowFilterDiv.find("[name='AppPress_RowFilter_Cancel']").click();
		e.stopPropogation();
		}
	}
}