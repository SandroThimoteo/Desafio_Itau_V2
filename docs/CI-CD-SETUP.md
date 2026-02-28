# CI/CD Setup Guide

## GitHub Actions Workflows

Este projeto possui dois workflows automáticos configurados:

### 1. Build, Test & Code Quality (`build-test-quality.yml`)

Executado em **push** e **pull requests** para `main` e `develop`.

**O que faz:**
- ✅ Restaura as dependências NuGet
- ✅ Compila o projeto em Release mode
- ✅ Executa todos os 99 testes unitários
- ✅ Coleta cobertura de testes (Cobertura format)
- ✅ Executa análise de código com SonarQube
- ✅ Faz upload de resultados para análise

**Configuração necessária:**

1. **SonarQube Token** (GitHub Secrets):
   ```bash
   Settings → Secrets and variables → Actions → New repository secret
   Nome: SONAR_TOKEN
   Valor: [token gerado no SonarQube]
   ```

2. **SonarQube Project** (opcional, para análise remota):
   - Forneça um token do SonarQube Cloud
   - O projeto key está definido em `sonar-project.properties`

### 2. Code Quality Checks (`code-quality-checks.yml`)

Executado em **push** e **pull requests** para `main` e `develop`.

**O que faz:**
- ✅ Verifica formatação de código (dotnet-format)
- ✅ Valida EditorConfig settings
- ✅ Detecta dependências vulneráveis
- ✅ Faz upload de relatório de vulnerabilidades

---

## SonarQube Setup Local

Para análise local de código:

```bash
# Instalar SonarQube Scanner (CLI)
dotnet tool install -g dotnet-sonarscanner

# Executar análise
dotnet sonarscanner begin \
  /k:"SandroThimoteo_Desafio_Itau_V2" \
  /o:"sandrothimoteo" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.login="[SEU_TOKEN]"

# Build e testes
dotnet build --configuration Release
dotnet test --configuration Release /p:CollectCoverage=true /p:CoverageFormat=cobertura

# Finalizar análise
dotnet sonarscanner end /d:sonar.login="[SEU_TOKEN]"
```

---

## Variáveis de Ambiente

### `DOTNET_VERSION`
- **Valor atual:** `8.0.x`
- **Descrição:** Versão do .NET usada nos workflows

### Secrets Necessários

| Nome | Descrição | Local |
|------|-----------|-------|
| `GITHUB_TOKEN` | Token automático do GitHub | Pré-configurado |
| `SONAR_TOKEN` | Token do SonarCloud | Gerar em Settings > Secrets |

---

## Como Visualizar Resultados

### Build & Tests
1. Acesse: [Actions](https://github.com/SandroThimoteo/Desafio_Itau_V2/actions)
2. Clique no workflow mais recente
3. Veja logs de cada step

### Code Coverage
1. Faça download do artifact `coverage-reports`
2. Abra em Visual Studio Code ou editor similar:
   ```bash
   # Converter para HTML (opcional)
   dotnet tool install -g dotnet-reportgenerator-globaltool
   reportgenerator -reports:"coverage.cobertura.xml" -targetdir:"coverage-report"
   ```

### SonarQube Analysis
- **Dashboard:** https://sonarcloud.io/dashboard?id=SandroThimoteo_Desafio_Itau_V2
- Métricas: Bugs, Code Smells, Security Hotspots, Coverage, Duplications

---

## Local Development

### Executar testes localmente com coverage:

```bash
cd tests/CompraProgramada.Tests

dotnet test \
  /p:CollectCoverage=true \
  /p:CoverageFormat=cobertura \
  /p:Exclude="[*.Tests]*"
```

### Verificar formatação:

```bash
# Instalar dotnet-format
dotnet tool install -g dotnet-format

# Verificar
dotnet format --verify-no-changes

# Corrigir automaticamente
dotnet format
```

### Scanear dependências vulneráveis:

```bash
dotnet list package --vulnerable
```

---

## Troubleshooting

### Build fails no GitHub Actions

**Problema:** `Restore concluído com erros`
- **Solução:** Verificar se `appsettings.json` tem connection strings válidas ou se há network issues

### SonarQube scan fails

**Problema:** `401 Unauthorized`
- **Solução:** Conferir se `SONAR_TOKEN` está correto em Secrets

**Problema:** Timeout na análise
- **Solução:** Aumentar `GHSAS_TIMEOUT` no workflow ou verificar coverage files

### Tests timeout

**Problema:** `dotnet test` leva mais de 2 minutos
- **Solução:** Alguns testes integram com COTAHIST file I/O, normal ser mais lento

---

## Próximos Passos

- [ ] Adicionar análise de performance (BenchmarkDotNet)
- [ ] Integrar code signing para releases
- [ ] Deploy automático em staging
- [ ] Adicionar security scanning (SAST/DAST)
- [ ] Dashboard de métricas (Grafana/DataDog)
