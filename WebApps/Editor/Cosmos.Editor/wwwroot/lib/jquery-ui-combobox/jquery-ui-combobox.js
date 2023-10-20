/*
 * jQuery UI Combobox based on:
 * https://jqueryui.com/autocomplete/#remote-jsonp
 * 
 * This widget requires the following to be loaded prior to this library:
 *  - jquery
 *  - jquery-ui
 */
(function ($) {
    $.widget("ui.combobox", {
        _create: function () {
            var wrapper = this.wrapper = $("<div />").addClass("input-group input-group-sm")
                , self = this;

            this.element.wrap(wrapper);

            this.element
                .addClass("ui-state-default")
                .autocomplete($.extend({
                    minLength: 0
                }, this.options));

            $("<a />")
                .insertAfter(this.element)
                .button({
                    icons: {
                        primary: "ui-icon-triangle-1-s"
                    },
                    text: false
                })
                .removeClass("ui-corner-all")
                .addClass("ui-corner-right ui-combobox-toggle")
                .click(function () {
                    if (self.element.autocomplete("widget").is(":visible")) {
                        self.element.autocomplete("close");
                        return;
                    }

                    $(this).blur();

                    self.element.autocomplete("search", "");
                    self.element.focus();
                });

            $("<a />")
                .insertAfter(this.element)
                .append('<i class="fa-solid fa-caret-down"></i>')
                .addClass("input-group-text combobox-toggle")
                .click(function () {
                    self.element.autocomplete("search", $("#combobox").val());
                });

        },
        destroy: function () {
            this.wrapper.remove();
            $.Widget.prototype.destroy.call(this);
        }
    });
})(jQuery);