class CliGpt56TerraCsharp < Formula
  desc "rclone-compatible file encryption CLI"
  homepage "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp"
  version "0.1.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp/releases/download/v0.1.0/cli-GPT-5.6-Terra-csharp-darwin-arm64.tar.gz"
      sha256 "REPLACE_ON_RELEASE"
    else
      url "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp/releases/download/v0.1.0/cli-GPT-5.6-Terra-csharp-darwin-amd64.tar.gz"
      sha256 "REPLACE_ON_RELEASE"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp/releases/download/v0.1.0/cli-GPT-5.6-Terra-csharp-linux-arm64.tar.gz"
      sha256 "REPLACE_ON_RELEASE"
    else
      url "https://github.com/llm-supermarket/cli-GPT-5.6-Terra-csharp/releases/download/v0.1.0/cli-GPT-5.6-Terra-csharp-linux-amd64.tar.gz"
      sha256 "REPLACE_ON_RELEASE"
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
