function formatEmployeeText(text) {
    return "<span>" + text.replace("@--@", "</span>&nbsp;-&nbsp;<span style='font-size:10px'>") + "</span>";
}
function formatEmployee(employee) {
    if (!employee.id) { return employee.text; }
    var idx = employee.text.indexOf("@--@");
    if (idx != -1)
        return $(formatEmployeeText(employee.text));
    return employee.text;
}

function MoveItemsToHeader() {
    try {
        if ($('#_SaveSource').length > 0) {
            $('#_SaveDestination a').attr('id', $('#_SaveSource a').attr('id'));
            $('#_SaveDestination a').attr('onclick', $('#_SaveSource a').attr('onclick'));
            $('#_SaveSource').html('');
        }
        else
            $('#_SaveDestination').html('');
    }
    catch (ex)
    { }
    try {
        if ($('#_PrintSource').length > 0) {
            $('#_PrintDestination a').attr('id', $('#_PrintSource a').attr('id'));
            $('#_PrintDestination a').attr('onclick', $('#_PrintSource a').attr('onclick'));
            $('#_PrintSource').html('');
        }
        else
            $('#_PrintDestination').html('');
    }
    catch (ex)
    { }
    try {
        if ($('#_HelpSource').length > 0) {
            $('#_HelpDestination a').attr('href', $('#_HelpSource a').attr('href'));
            $('#_HelpSource').html('');
        }
        else
            $('#_HelpDestination').html('');
    }
    catch (ex)
    { }
}