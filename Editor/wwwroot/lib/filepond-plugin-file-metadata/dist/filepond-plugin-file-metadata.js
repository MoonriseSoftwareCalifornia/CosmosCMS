/*!
 * FilePondPluginFileMetadata 1.0.8
 * Licensed under MIT, https://opensource.org/licenses/MIT/
 * Please visit https://pqina.nl/filepond/ for details.
 */

/* eslint-disable */

(function(global, factory) {
  typeof exports === 'object' && typeof module !== 'undefined'
    ? (module.exports = factory())
    : typeof define === 'function' && define.amd
    ? define(factory)
    : ((global = global || self),
      (global.FilePondPluginFileMetadata = factory()));
})(this, function() {
  'use strict';

  var plugin = function plugin(_ref) {
    var addFilter = _ref.addFilter,
      utils = _ref.utils;

    // get quick reference to Type utils
    var Type = utils.Type;

    // setup attribute mapping for accept
    addFilter('SET_ATTRIBUTE_TO_OPTION_MAP', function(map) {
      return Object.assign(map, {
        '^fileMetadata': {
          group: 'fileMetadataObject'
        }
      });
    });

    addFilter('DID_LOAD_ITEM', function(item, _ref2) {
      var query = _ref2.query;
      return new Promise(function(resolve) {
        if (!query('GET_ALLOW_FILE_METADATA')) {
          return resolve(item);
        }

        // get default object and add as metadata
        var data = query('GET_FILE_METADATA_OBJECT');
        if (typeof data === 'object' && data !== null) {
          Object.keys(data).forEach(function(key) {
            item.setMetadata(key, data[key], true);
          });
        }

        resolve(item);
      });
    });

    return {
      options: {
        // Enable or disable file renaming
        allowFileMetadata: [true, Type.BOOLEAN],

        // A metadata object to add to all files
        fileMetadataObject: [null, Type.OBJECT]
      }
    };
  };

  // fire pluginloaded event if running in browser, this allows registering the plugin when using async script tags
  var isBrowser =
    typeof window !== 'undefined' && typeof window.document !== 'undefined';
  if (isBrowser) {
    document.dispatchEvent(
      new CustomEvent('FilePond:pluginloaded', { detail: plugin })
    );
  }

  return plugin;
});
