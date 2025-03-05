/**
 * encryptData
 * @param {any} plainText The text to encrypt.
 * @returns
 */
function encryptData(plainText) {
    if (typeof plainText === 'undefined' || plainText === null || plainText === "") {
        return "";
    }
    const keyText = "1234567890123456";
    const key = CryptoJS.enc.Utf8.parse(keyText); // 16 bytes key for AES-128
    const iv = CryptoJS.enc.Utf8.parse(keyText);
    // Encrypt the plaintext
    const encrypted = CryptoJS.AES.encrypt(plainText, key, {
        iv: iv,
        padding: CryptoJS.pad.Pkcs7,
        mode: CryptoJS.mode.CBC
    });

    return encrypted.toString();
}