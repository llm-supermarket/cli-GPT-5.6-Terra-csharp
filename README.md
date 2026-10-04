# rclone-encrypt-GPT-5.6-Terra

A small CLI tool that encrypts and decrypts using the rclone encryption defaults.

It is wire-compatible with rclone crypt's standard filename encryption and encrypted file format. Releases are self-contained native executables; end users do not need to install .NET or rclone.

## Installation

**Homebrew (macOS/Linux)**
```bash
brew tap llm-supermarket/cli-GPT-5.6-Terra-csharp https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp
brew install cli-GPT-5.6-Terra-csharp
```

**Scoop (Windows)**
```powershell
scoop bucket add cli-GPT-5.6-Terra-csharp https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp
scoop install cli-GPT-5.6-Terra-csharp
```

## Examples usage

### Encrypt a file

```bash
cli-GPT-5.6-Terra-csharp encrypt -i TEST_FILE.txt
```

The CLI prompts for a password and an optional salt. Without `-o`, it creates an encrypted file beside the input using rclone's lower-case, unpadded base32 filename encoding.

### Decrypt an rclone crypt file

```bash
cli-GPT-5.6-Terra-csharp decrypt -i kr9tu4e1da4u3nifdd99g9tf5o
```

Without `-o`, the CLI decrypts the filename and writes the result beside the encrypted input.

### Choose destinations and a filename encoding

```bash
cli-GPT-5.6-Terra-csharp encrypt -i TEST_FILE.txt -o backup.bin --filename-encoding base64
cli-GPT-5.6-Terra-csharp decrypt -i backup.bin -o TEST_FILE.txt --filename-encoding base64
```

Supported filename encodings are `base32` (default, matching rclone) and `base64` (rclone URL-safe base64).

### Non-interactive automation

```bash
cli-GPT-5.6-Terra-csharp encrypt -i TEST_FILE.txt --password "$RCLONE_CRYPT_PASSWORD" --salt "$RCLONE_CRYPT_SALT"
```

`--password` emits a warning because command arguments can be retained in terminal history and exposed by process listings. Prefer the interactive prompt or an environment variable, and remove any command containing a literal password from shell history.

## Building from source

Requires the .NET 10 SDK.

```bash
dotnet test RcloneEncrypt.slnx
dotnet publish src/RcloneEncrypt/RcloneEncrypt.csproj -c Release -r win-x64
```

## Releases

Pushing a `vX.Y.Z` tag triggers [Build and Release](.github/workflows/build-release.yml). It publishes self-contained binaries for Linux and macOS (amd64/arm64) and Windows (amd64), creates a GitHub Release, then updates the Scoop manifest and Homebrew formula in this repository.
