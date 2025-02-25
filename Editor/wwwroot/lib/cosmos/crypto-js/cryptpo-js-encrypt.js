/*
Encryption helpler class for CryptoJS that is setup to work with C# to decrypt.
This is derived from the fine work by Daniel Little
Repo: https://github.com/daniellittledev/CryptoTest
*/




const wordArraySubArray = function (array, index, length) {
    var hex = array.toString(CryptoJS.enc.Hex);
    var subHex = hex.substr(index * 2, length * 2);
    return CryptoJS.enc.Hex.parse(subHex);
};

const getKeyAndIV = function (password, salt) {

    var ivBitLength = 128;
    var keyBitLength = 256;

    var ivByteLength = ivBitLength / 8;
    var keyByteLength = keyBitLength / 8;

    var ivWordLength = ivBitLength / 32;
    var keyWordLength = keyBitLength / 32;

    var totalWordLength = ivWordLength + keyWordLength;

    var iterations = 1000;
    var allBits = CryptoJS.PBKDF2(password, salt, { keySize: totalWordLength, iterations: iterations });

    var iv128Bits = wordArraySubArray(allBits, 0, ivByteLength);
    var key256Bits = wordArraySubArray(allBits, ivByteLength, keyByteLength);

    return {
        iv: iv128Bits,
        key: key256Bits
    };
};

function encryptData(password, plainText) {
    const salt = CryptoJS.lib.WordArray.random(bytesInSalt);
    const skey = getKeyAndIV(password, salt);
    const bytesInSalt = 128 / 8;
    const data = CryptoJS.AES.encrypt(plainText, skey.key, { iv: skey.iv, padding: CryptoJS.pad.Pkcs7 }); // , format: JsonFormatter
    const key = data.key.toString(CryptoJS.enc.Base64);
    const iv = data.iv.toString(CryptoJS.enc.Base64);

    var dataToSend = {
        password: password,
        salt: salt.toString(CryptoJS.enc.Base64),
        ciphertext: data.ciphertext.toString(CryptoJS.enc.Base64),
        key: key,
        iv: iv,
    }

    return dataToSend;
}