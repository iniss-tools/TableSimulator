namespace TableSimulator.Application.Https;

/// <summary>Certifikat na stiahnutie.</summary>
/// <param name="Content">certifikat v tvare DER</param>
/// <param name="FileName">meno suboru</param>
/// <param name="Authority">je to certifikacna autorita (zariadenie ho ma nainstalovat ako doveryhodnu autoritu)</param>
public sealed record HttpsCertificateFile(byte[] Content, string FileName, bool Authority);
