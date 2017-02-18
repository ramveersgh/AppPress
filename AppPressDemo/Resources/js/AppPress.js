function setCookie(cname, cvalue, exdays) {
    var d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    var expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + "; " + expires + "; path=/";
}

function getCookie(cname) {
    var name = cname + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1);
        if (c.indexOf(name) == 0) return c.substring(name.length, c.length);
    }
    return "";
}
function ResetTableScrollHeaders() {
    // Change the selector if needed
    var tablesFound = false;
    var $table = $('table.TableScroll');
    $table.each(function (ii, tbl) {
        tablesFound = true;
        var $bodyCells = $(tbl).find('tbody tr:first').children(),
		$headerCells = $(tbl).find('thead tr:last').children(),
		colWidth, headerWidth;
        $bodyCells = $bodyCells.filter(function () { return $(this).is(':visible') });
        $headerCells = $headerCells.filter(function () { return $(this).is(':visible') });
        // Get the tbody columns width array
        colWidth = $bodyCells.map(function () {
            return $(this).outerWidth();
        }).get();
        // Get the thad columns width array
        headerWidth = $headerCells.map(function () {
            return $(this).outerWidth();
        }).get();
        // Set the width of thead columns
        $headerCells.each(function (i, v) {
            var width = Math.max(colWidth[i], headerWidth[i]) + "px";

            $($bodyCells[i]).css({
                "min-width": width,
                "max-width": width,
                "width": width
            });
            $(v).css({
                "min-width": width,
                "max-width": width,
                "width": width
            });
        });
    });
    return tablesFound;
}


function _AppPressRefresh() {
    //iCheck for checkbox and radio inputs
    $('input[type="checkbox"], input[type="radio"]').iCheck({
        checkboxClass: 'icheckbox_minimal-grey',
        radioClass: 'iradio_minimal-grey',
        increaseArea: '-5%' // optional
    }).on('ifToggled', function (event) {
        $(this).trigger('onclick');
    });

    $.fn.select2.amd.require([
      'select2/data/array',
      'select2/utils'
    ], function (ArrayData, Utils) {
        function CustomData($element, options) {
            CustomData.__super__.constructor.call(this, $element, options);
        }

        Utils.Extend(CustomData, ArrayData);

        CustomData.prototype.query = function (params, callback) {
            var id = this.$element[0].id;
            var s = id.split(":");
            if (s.length < 5) {
                alert("Invalid Object Id:" + obj.id);
            }
            var formFieldType = s[1];
            var formDefId = s[2];
            var fieldDefId = s[3];
            var formId = s[4];
            AutoCompleteCall(defaultInstanceId, formId, fieldDefId, params.term, callback);
        };
        var select2s = $("select.select2");
        for (i = 0; i < select2s.length; ++i) {
            var select = select2s[i];
            var params = {};
            var s = select.id.split(":");
            var formFieldType = s[1];
            var formDefId = s[2];
            var fieldDefId = s[3];
            var formData = FindFormData(formDefId, s[4]);
            var fieldValue = GetFieldValue(formData, fieldDefId);
            var formField = FindFormField(fieldValue);
            var style = formField.Style;
            if (style == 9) {
                params.dataAdapter = CustomData;
                params.templateResult = formatEmployee;
                params.templateSelection = formatEmployee;
            }
            else
                params.minimumResultsForSearch = -1;
            $(select).select2(params);
        }
    });
    $('textarea').autosize();
    // Adjust the width of thead cells when window resizes
    $(window).resize(ResetTableScrollHeaders).resize(); // Trigger resize handler

}

function SkinInit() {
    // back button should remove overlay if any
    $(window).bind("pageshow", function (event) {
        disablePageCount = 0;
        if ($("#overlay").length > 0)
            $("#overlay").hide();
    });

    $(".scrollToTop").hide();
    $(function () {
        $(window).scroll(function () {
            if ($(this).scrollTop() > 300) {
                $('.scrollToTop').slideDown();
            }
            else {
                $('.scrollToTop').slideUp();
            }
        });

        $('.scrollToTop').click(function (e) {
            e.preventDefault();
            $('body,html').animate({ scrollTop: 0 }, 500);
        });

    });
}

function AppPressOpenMenu(formName)
{
    $('a[formName="' + formName + '"]').parent().addClass('active').parent().parent('.treeview').addClass('active').parent().parent('.treeview').addClass('active');
    $('.apppress-menu-open > ul').css('display', 'block');
    $('.apppress-menu-open > a > .pull-right-container > .fa-angle-left').css('transform', 'rotate(-90deg)');
}

