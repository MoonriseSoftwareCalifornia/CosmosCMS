/**
 * encryptData
 * @param {any} plainText The text to encrypt.
 * @param {any} keyText The key to use for encryption (default is 1234567890123456).
 * @returns
 */
function encryptData(plainText, keyText) {
    if (typeof plainText === 'undefined' || plainText === null || plainText === "") {
        return "";
    }
    if (typeof keyText === 'undefined' || keyText === null || keyText === "") {
        keyText = '1234567890123456';
    }
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