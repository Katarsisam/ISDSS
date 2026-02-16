namespace ISDSS.Application.Abstractions;

public interface ICryptoService
{
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
}
