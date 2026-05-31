import { Component } from '@angular/core';

@Component({
  selector: 'app-qrcode-pix',
  templateUrl: './qrcode-pix.component.html',
  styleUrls: ['./qrcode-pix.component.css']
})
export class QrCodePixComponent {
  isOpen = false;

  open() {
    this.isOpen = true;
  }

  close() {
    this.isOpen = false;
  }
}
