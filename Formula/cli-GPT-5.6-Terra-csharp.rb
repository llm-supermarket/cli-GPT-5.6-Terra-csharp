class CliGpt56TerraCsharp < Formula
  desc "rclone-compatible file encryption CLI"
  homepage "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp"
  version "0.1.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp/releases/download/v0.1.0/cli-GPT-5.6-Terra-csharp-darwin-arm64.tar.gz"
      sha256 "25ad1ed6cfdf741d9304cded05358af1e757ed4dc89736169752c213d14c3edd"
    else
      url "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp/releases/download/v0.1.0/cli-GPT-5.6-Terra-csharp-darwin-amd64.tar.gz"
      sha256 "fdb70a7bc9be633ea0bbc473c236e9a4aa780f39cd5e99a39870e0276cfbc024"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp/releases/download/v0.1.0/cli-GPT-5.6-Terra-csharp-linux-arm64.tar.gz"
      sha256 "a26ad972be210ae10b074dd54f52e7c03c2ea30db879987ea27088bfca0a5839"
    else
      url "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp/releases/download/v0.1.0/cli-GPT-5.6-Terra-csharp-linux-amd64.tar.gz"
      sha256 "7a46f8b38a1c8a30ae245634aefa848f23c8e48f2409fae639b93bc03858baf0"
    end
  end

  def install
    bin.install "cli-GPT-5.6-Terra-csharp-darwin-arm64" => "cli-GPT-5.6-Terra-csharp" if OS.mac? && Hardware::CPU.arm?
    bin.install "cli-GPT-5.6-Terra-csharp-darwin-amd64" => "cli-GPT-5.6-Terra-csharp" if OS.mac? && !Hardware::CPU.arm?
    bin.install "cli-GPT-5.6-Terra-csharp-linux-arm64" => "cli-GPT-5.6-Terra-csharp" if OS.linux? && Hardware::CPU.arm?
    bin.install "cli-GPT-5.6-Terra-csharp-linux-amd64" => "cli-GPT-5.6-Terra-csharp" if OS.linux? && !Hardware::CPU.arm?
  end

  test do
    assert_match "cli-GPT-5.6-Terra-csharp #{version}", shell_output("#{bin}/cli-GPT-5.6-Terra-csharp --version")
  end
end