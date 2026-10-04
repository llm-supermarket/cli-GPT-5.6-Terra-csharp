param(
    [Parameter(Mandatory = $true)] [string] $Version,
    [Parameter(Mandatory = $true)] [string] $ArtifactsPath
)

$version = $Version.TrimStart('v')
$name = 'cli-GPT-5.6-Terra-csharp'
$repo = 'llm-supermarket/cli-GPT-5.6-Terra-csharp'
$windows = Join-Path $ArtifactsPath "$name-windows-amd64.exe"
$hash = (Get-FileHash -LiteralPath $windows -Algorithm SHA256).Hash.ToLowerInvariant()
$manifest = Get-Content "$PSScriptRoot/$name.json" -Raw | ConvertFrom-Json
$manifest.version = $version
$manifest.architecture.'64bit'.url = "https://github.com/$repo/releases/download/v$version/$name-windows-amd64.exe"
$manifest.architecture.'64bit'.hash = $hash
$manifest | ConvertTo-Json -Depth 10 | Set-Content "$PSScriptRoot/$name.json" -NoNewline

$sha = @{}
foreach ($platform in 'darwin-amd64', 'darwin-arm64', 'linux-amd64', 'linux-arm64') {
    $sha[$platform] = (Get-FileHash -LiteralPath (Join-Path $ArtifactsPath "$name-$platform.tar.gz") -Algorithm SHA256).Hash.ToLowerInvariant()
}
$base = "https://github.com/$repo/releases/download/v$version"
$formula = @"
class CliGpt56TerraCsharp < Formula
  desc "rclone-compatible file encryption CLI"
  homepage "https://github.com/$repo"
  version "$version"

  on_macos do
    if Hardware::CPU.arm?
      url "$base/$name-darwin-arm64.tar.gz"
      sha256 "$($sha['darwin-arm64'])"
    else
      url "$base/$name-darwin-amd64.tar.gz"
      sha256 "$($sha['darwin-amd64'])"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "$base/$name-linux-arm64.tar.gz"
      sha256 "$($sha['linux-arm64'])"
    else
      url "$base/$name-linux-amd64.tar.gz"
      sha256 "$($sha['linux-amd64'])"
    end
  end

  def install
    bin.install "$name-darwin-arm64" => "$name" if OS.mac? && Hardware::CPU.arm?
    bin.install "$name-darwin-amd64" => "$name" if OS.mac? && !Hardware::CPU.arm?
    bin.install "$name-linux-arm64" => "$name" if OS.linux? && Hardware::CPU.arm?
    bin.install "$name-linux-amd64" => "$name" if OS.linux? && !Hardware::CPU.arm?
  end

  test do
    assert_match "$name #{version}", shell_output("#{bin}/$name --version")
  end
end
"@
Set-Content "$PSScriptRoot/Formula/$name.rb" $formula -NoNewline
