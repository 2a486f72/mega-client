* Proper encryption key management system, instead of ad-hoc code.
* Fix-up all the special casing logic due to funky messaging design of Mega.
* Validate RSA key & encrypted data length where it is encountered (e.g. share/inbox keys).
* Replace RSA keys with AES keys automatically.
* Consume shared items.
* Create and manage shares.