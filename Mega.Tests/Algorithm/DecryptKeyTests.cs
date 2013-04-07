namespace Mega.Tests.Algorithm
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class DecryptKeyTests
	{
		[TestMethod]
		public void DecryptKey_ForAesKey_SeemsToWork()
		{
			var passwordKey = Algorithms.DeriveAesKey("creation");
			var encryptedKey = Algorithms.Base64Decode("aXaYqXimDxtnOx2OunCXgg");

			var key = Algorithms.AesDecryptKey(encryptedKey, passwordKey);

			var a32 = Algorithms.BytesToA32(key);

			Assert.AreEqual(2100112535, a32[0]);
			Assert.AreEqual(963111541, a32[1]);
			Assert.AreEqual(241751129, a32[2]);
			Assert.AreEqual(1081887932, a32[3]);
		}

		[TestMethod]
		public void DecryptKey_ForRsaKey_SeemsToWork()
		{
			var passwordKey = Algorithms.DeriveAesKey("creation");
			var encryptedMasterKey = Algorithms.Base64Decode("aXaYqXimDxtnOx2OunCXgg");

			var masterKey = Algorithms.AesDecryptKey(encryptedMasterKey, passwordKey);

			var encryptedRsaKey = Algorithms.Base64Decode("3Y1jUdtxUHgx9tK2eslnoSIORipVFHMainkCIWH6fRFFcb6Z8KC_ipK9Jeebs5u7saBbDDUR6atag2BBP3LIzLEvEHAVmjgAZaKbX0dfZKVI4wkGAmM4-8s3z9ke_EIveslG41TvPygAK-seHYfkptkp-1eZdVrZmdqlumagqcZDksB24vr0CrD7hYD9HSKOPTjFSEFXyoK10N9iB1DBqNhw-2cVLqysUf4FTMyG1Ewe7fMvKkJdes2lU2mQue_5qxj890M2Lhq-5SS_rUqaWLkCOnWTaLqsCARB09yshyqCwA2eNecNhO12nB28D5hTIabjYUmvQAZvodrTSKCozvBTcDBiMFukBITtwl-lpRBUD5GUQ-T2oP-ozyEySWUzFh0_h2xKUVvwmAoH44VZ63dwUETeP0TMfoJigJcmLKaSwE1m9ur3x0iVC3asYo-qGG9f8Xqm8_aclcGzGQ8gid2vy5kpQrAk50L1m1PnvWV3t_Bqq8ovWfGybWVFaoPykHkPf2fbJACeAHtq1kKbvCh1P8LPEWpdcM6I6wisw_UD1EwxBuzMSo0JPqEHMsYKoRy42muRv8X81_OYRkVVCEfQaoRneBBCQismhhduCRRw_aErVZl2fxBQJ66yUDCpYz2Pix_Qpk9CuN_tIW0GoTIlDXM-Xg3Ew9BGrhkrWz283ZRMGZ9xiPNCOjNkWGZJ25d-HirmLfWj_z2kg12Ix7uK7ZOsOU5v0zTcnc1YfriXO7ljZHchoToU_XK1I-Lqi2H2nRqwxKk1wEePz_nPDveU_0oy-HWxoGOnUptNyb0rpYb_zZ0wfyfcXR_a_b9omOBaFEdoz5G5B2jdXqjYXKYYd4MV3kFgQAQBkQIficc");

			var rsaKey = Algorithms.AesDecryptKey(encryptedRsaKey, masterKey);

			var a32 = Algorithms.BytesToA32(rsaKey);

			Assert.AreEqual(67163145, a32[0]);
			Assert.AreEqual(214117842, a32[1]);
			Assert.AreEqual(1035103535, a32[2]);
			Assert.AreEqual(-113883837, a32[3]);
			Assert.AreEqual(-842084272, a32[4]);
			Assert.AreEqual(713831477, a32[5]);
			Assert.AreEqual(2135643838, a32[6]);
			Assert.AreEqual(2146402227, a32[7]);
			Assert.AreEqual(1198993090, a32[8]);
			Assert.AreEqual(85172617, a32[9]);
		}
	}
}