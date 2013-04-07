* Fix-up all the special casing logic due to funky messaging design of Mega.
* Replace RSA keys with AES keys automatically.
* Understand share access rights and ownership.
* Create and manage shares.
* Tests for share functionality.
* Improve PRNG (problem is that .NET and WinRT both offer a secure one, but it is not the same one so it cannot be used in a portable library context).