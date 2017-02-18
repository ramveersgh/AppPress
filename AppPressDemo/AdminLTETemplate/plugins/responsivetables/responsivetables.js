(function ($) {
  $.fn.ResponsiveTable = function (options) {

    var settings = $.extend({
      breakpoint: 640
    }, options);

    var _this = $(this);

    Resize = function(_this) {
      if (window.innerWidth <= settings.breakpoint) {
        _this.each(function () {
          if (!_this.hasClass("responsive-table")) {
            var firstRow = _this.find("tr:first"),
              remainingRows = _this.find("tr").not(':first'),
              cols = [];

            var headers = firstRow.children('th').length != 0 ? firstRow.children('th') : firstRow.children('td');

            headers.each(function (index) {
              cols[index] = $(this).text();
            });

            remainingRows.each(function () {
              $(this).children('td').each(function (index) {
                $(this).attr("data-rt-label", cols[index]);
              });
            });

            _this.addClass("responsive-table");
          }
        });
      }
      else {
        if (_this.hasClass("responsive-table")) {
          _this.toggleClass("responsive-table")
        }
      }
    };

    Resize(_this);

    $(window).resize(function () {
      Resize(_this);
    });

  };
}(jQuery));