/**
 * encryptData
 * @param {any} plainText The text to encrypt.
 * @returns encrypted data.
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

/**
 * decryptData
 * @param {any} encrypedText The text to decrypt.
 * @returns decrypted data.
 */
function decryptData(encrypedText) {
    if (typeof encrypedText === 'undefined' || encrypedText === null || encrypedText === "") {
        return "";
    }
    const keyText = "1234567890123456";
    const key = CryptoJS.enc.Utf8.parse(keyText); // 16 bytes key for AES-128
    const iv = CryptoJS.enc.Utf8.parse(keyText);
    // Decrypt the encrypted text
    const decrypted = CryptoJS.AES.decrypt(encrypedText, key, {
        iv: iv,
        padding: CryptoJS.pad.Pkcs7,
        mode: CryptoJS.mode.CBC
    });
    return decrypted.toString(CryptoJS.enc.Utf8);
}