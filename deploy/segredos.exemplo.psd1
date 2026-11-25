# Modelo dos dados de deploy no MonsterASP.NET.
# Copie para "segredos.local.psd1" (ignorado pelo git) e preencha. NUNCA commite senhas.
@{
    # Painel (admin.monsterasp.net) > Websites > seu site > Deploy > card "Visual Studio access"
    # (ligue WebDeploy = Enabled para os dados aparecerem)
    SiteId           = 'siteXXXXX'
    WebDeployUrl     = 'https://siteXXXXX.siteasp.net:8172/msdeploy.axd?site=siteXXXXX'
    WebDeployLogin   = 'siteXXXXX'
    WebDeploySenha   = ''

    # Endereço público do site (Overview do site). No plano Free o HTTPS só vem a pedido do suporte.
    SiteUrl          = 'http://SEU-SITE.runasp.net'

    # O banco precisa ser MS SQL (não MySQL). Painel > Databases > seu banco > Users > Connection string
    # "Local access" (usada pelo site):
    ConnectionString = 'Server=dbXXXXX.databaseasp.net; Database=dbXXXXX; User Id=dbXXXXX; Password=SENHA; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;'
    # "Remote access" (opcional; com acesso remoto ligado, o script testa o login antes de publicar):
    ConnectionStringRemota = ''
}
