/*!
 * FilePondPluginFileMetadata 1.0.8
 * Licensed under MIT, https://opensource.org/licenses/MIT/
 * Please visit https://pqina.nl/filepond/ for details.
 */

/* eslint-disable */

const plugin = ({ addFilter, utils }) => {
  // get quick reference to Type utils
  const { Type } = utils;

  // setup attribute mapping for accept
  addFilter('SET_ATTRIBUTE_TO_OPTION_MAP', map =>
    Object.assign(map, {
      '^fileMetadata': {
        group: 'fileMetadataObject'
      }
    })
  );

  addFilter(
    'DID_LOAD_ITEM',
    (item, { query }) =>
      new Promise(resolve => {
        if (!query('GET_ALLOW_FILE_METADATA')) {
          return resolve(item);
        }

        // get default object and add as metadata
        const data = query('GET_FILE_METADATA_OBJECT');
        if (typeof data === 'object' && data !== null) {
          Object.keys(data).forEach(key => {
            item.setMetadata(key, data[key], true);
          });
        }

        resolve(item);
      })
  );

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
const isBrowser =
  typeof window !== 'undefined' && typeof window.document !== 'undefined';
if (isBrowser) {
  document.dispatchEvent(
    new CustomEvent('FilePond:pluginloaded', { detail: plugin })
  );
}

export default plugin;
