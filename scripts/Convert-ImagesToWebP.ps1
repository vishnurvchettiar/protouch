<#
.SYNOPSIS
    Converts all JPEG/JPG images under wwwroot/images/ to WebP format.

.DESCRIPTION
    Recursively discovers *.jpeg and *.jpg files under wwwroot/images/,
    converts each to a .webp file in the same directory, and skips files
    where a .webp already exists and is newer than the source.

    Requires ImageMagick (magick) to be installed and on PATH.
    Installation: https://imagemagick.org/script/download.php

    Alternatively, use cwebp from Google's WebP tools:
    https://developers.google.com/speed/webp/docs/cwebp

.PARAMETER SourceRoot
    Root directory to search. Defaults to 'wwwroot/images' relative to script location.

.PARAMETER Quality
    WebP quality factor (0-100). Default: 82.

.PARAMETER DryRun
    If specified, reports what would be converted without actually converting.

.EXAMPLE
    .\Convert-ImagesToWebP.ps1
    .\Convert-ImagesToWebP.ps1 -Quality 90 -DryRun
#>

param(
    [string]$SourceRoot = "",
    [int]$Quality = 82,
    [switch]$DryRun
)

# ── Resolve source root ─────────────────────────────────────
if (-not $SourceRoot) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectRoot = Split-Path -Parent $scriptDir
    $SourceRoot = Join-Path $projectRoot "ClinicApp\wwwroot\images"
}

if (-not (Test-Path $SourceRoot)) {
    Write-Error "Source root not found: $SourceRoot"
    exit 1
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ProTouch — JPEG → WebP Conversion Script" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Source : $SourceRoot"
Write-Host "  Quality: $Quality"
if ($DryRun) { Write-Host "  Mode   : DRY RUN (no files will be written)" -ForegroundColor Yellow }
Write-Host ""

# ── Detect converter ────────────────────────────────────────
$useMagick = $false
$useCwebp  = $false

if (Get-Command magick -ErrorAction SilentlyContinue) {
    $useMagick = $true
    Write-Host "  ✓ Found: ImageMagick (magick)" -ForegroundColor Green
} elseif (Get-Command cwebp -ErrorAction SilentlyContinue) {
    $useCwebp = $true
    Write-Host "  ✓ Found: cwebp" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "  ✗ No converter found." -ForegroundColor Red
    Write-Host ""
    Write-Host "  Please install one of the following:" -ForegroundColor Yellow
    Write-Host "    • ImageMagick: https://imagemagick.org/script/download.php" -ForegroundColor Yellow
    Write-Host "      Then run: magick convert input.jpg -quality 82 output.webp" -ForegroundColor Gray
    Write-Host "    • cwebp:      https://developers.google.com/speed/webp/docs/cwebp" -ForegroundColor Yellow
    Write-Host "      Then run: cwebp -q 82 input.jpg -o output.webp" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Alternatively, use an online service or Squoosh CLI:" -ForegroundColor Yellow
    Write-Host "    npx @squoosh/cli --webp '{quality:82}' *.jpg" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

Write-Host ""

# ── Discover source images ──────────────────────────────────
$jpegFiles = Get-ChildItem -Path $SourceRoot -Recurse -Include "*.jpeg","*.jpg" | Sort-Object FullName

if ($jpegFiles.Count -eq 0) {
    Write-Host "  No JPEG files found under $SourceRoot" -ForegroundColor Yellow
    exit 0
}

Write-Host "  Found $($jpegFiles.Count) JPEG file(s) to process."
Write-Host ""

# ── Counters ────────────────────────────────────────────────
$converted = 0
$skipped   = 0
$failed    = 0

foreach ($file in $jpegFiles) {
    $webpPath = [System.IO.Path]::ChangeExtension($file.FullName, ".webp")
    $relPath  = $file.FullName.Replace($SourceRoot, "").TrimStart('\','/')

    # Skip if .webp exists and is newer than source
    if (Test-Path $webpPath) {
        $webpFile = Get-Item $webpPath
        if ($webpFile.LastWriteTime -ge $file.LastWriteTime) {
            Write-Host "  SKIP    $relPath  (webp up-to-date)" -ForegroundColor DarkGray
            $skipped++
            continue
        }
    }

    if ($DryRun) {
        Write-Host "  DRY-RUN $relPath → $([System.IO.Path]::GetFileName($webpPath))" -ForegroundColor Cyan
        $converted++
        continue
    }

    # Convert
    try {
        if ($useMagick) {
            $result = & magick convert "$($file.FullName)" -quality $Quality "$webpPath" 2>&1
        } else {
            $result = & cwebp -q $Quality "$($file.FullName)" -o "$webpPath" 2>&1
        }

        if ($LASTEXITCODE -eq 0) {
            $srcKb  = [math]::Round($file.Length / 1024, 1)
            $webpKb = [math]::Round((Get-Item $webpPath).Length / 1024, 1)
            $saving = [math]::Round((1 - $webpKb / $srcKb) * 100, 0)
            Write-Host "  OK      $relPath  ($srcKb KB → $webpKb KB, ${saving}% saved)" -ForegroundColor Green
            $converted++
        } else {
            Write-Host "  FAIL    $relPath  (exit $LASTEXITCODE)" -ForegroundColor Red
            if ($result) { Write-Host "          $result" -ForegroundColor Red }
            $failed++
        }
    } catch {
        Write-Host "  FAIL    $relPath  ($($_.Exception.Message))" -ForegroundColor Red
        $failed++
    }
}

# ── Summary ─────────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
$dryLabel = if ($DryRun) { " (dry run)" } else { "" }
Write-Host "  Summary$dryLabel"
Write-Host "  Converted : $converted"
Write-Host "  Skipped   : $skipped  (already up-to-date)"
Write-Host "  Failed    : $failed"
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($failed -gt 0) { exit 1 }
exit 0
