import { Component, ViewChild, HostListener } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { PdfResult } from './models/pdf-result.model';
import { QrCodePixComponent } from './components/qrcode-pix/qrcode-pix.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'Docs2Pdf';
  pdfResults: PdfResult[] = [];
  selectedPdfIndex = 0;
  errorMessage = '';
  loading = false;
  trustedUrl?: SafeResourceUrl | null = null;

  @ViewChild(QrCodePixComponent) qrCodeModal?: QrCodePixComponent;

  // true quando a largura da viewport for igual ou menor que 768px
  isMobile = window.innerWidth <= 768;

  constructor(private sanitizer: DomSanitizer) {}

  @HostListener('window:resize')
  onResize() {
    this.isMobile = window.innerWidth <= 768;
  }

  getViewLabel(index: number) {
    if (this.isMobile) {
      return 'Abrir';
    }

    return this.selectedPdfIndex === index ? 'Visualizando' : 'Visualizar';
  }

  openOrPreview(index: number) {
    const pdf = this.pdfResults[index];
    if (!pdf) return;

    if (this.isMobile) {
      // em mobile, abrir em nova aba (fallback ao data URL)
      try {
        window.open(pdf.data, '_blank', 'noopener');
      } catch (e) {
        // se abrir falhar, forçar download abrindo a url no mesmo contexto
        window.location.href = pdf.data;
      }
      return;
    }

    this.selectPreview(index);
  }

  openQrCode() {
    if (this.qrCodeModal) {
      this.qrCodeModal.open();
    }
  }

  onPdfReady(results: PdfResult[]) {
    this.pdfResults = results;
    this.selectedPdfIndex = 0;
    this.errorMessage = '';
    this.loading = false;
    this.trustedUrl = results.length ? this.sanitizer.bypassSecurityTrustResourceUrl(results[0].data) : null;
  }

  onError(message: string) {
    this.errorMessage = message;
    this.pdfResults = [];
    this.selectedPdfIndex = 0;
    this.loading = false;
    this.trustedUrl = null;
  }

  onLoading(loading: boolean) {
    this.loading = loading;
  }

  selectPreview(index: number) {
    this.selectedPdfIndex = index;
    this.trustedUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.pdfResults[index].data);
  }
}
