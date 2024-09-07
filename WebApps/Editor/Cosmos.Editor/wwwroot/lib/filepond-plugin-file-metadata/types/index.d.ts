// @ts-ignore
import { FilePondOptions } from "filepond";

declare module "filepond" {
  export interface FilePondOptions {
    /** Enable or disable the filemetadata object */
    allowFileMetadata?: boolean;

    /** The object that is used to set the initial metadata object of each file item */
    fileMetadataObject?: undefined | object | null;
  }
}
