import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

import { AppComponent } from './app.component';
import { FileDropComponent } from './components/file-drop/file-drop.component';
import { ModalComponent } from './components/modal/modal.component';
import { QrCodePixComponent } from './components/qrcode-pix/qrcode-pix.component';

import { DragDropModule } from '@angular/cdk/drag-drop';

@NgModule({
  declarations: [AppComponent, FileDropComponent, ModalComponent, QrCodePixComponent],
  imports: [BrowserModule, FormsModule, HttpClientModule, DragDropModule],
  bootstrap: [AppComponent],
})
export class AppModule {}
