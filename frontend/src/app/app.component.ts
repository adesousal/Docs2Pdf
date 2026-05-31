import { Component, ViewChild } from '@angular/core';
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

  constructor(private sanitizer: DomSanitizer) {}

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
