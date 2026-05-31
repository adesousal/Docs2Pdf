import { Component, EventEmitter, Output } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConvertService } from '../../services/convert.service';
import { ModalService } from '../../services/modal.service';
import { PdfResult } from '../../models/pdf-result.model';
import { CdkDragDrop, moveItemInArray} from '@angular/cdk/drag-drop';

interface SelectedFile {
  file: File;
  id: string;
  preview?: string;
  isImage?: boolean;
}

@Component({
  selector: 'app-file-drop',
  templateUrl: './file-drop.component.html',
  styleUrls: ['./file-drop.component.css']
})
export class FileDropComponent {
  @Output() pdfReady = new EventEmitter<PdfResult[]>();
  @Output() errorMessage = new EventEmitter<string>();
  @Output() loadingState = new EventEmitter<boolean>();

  selectedFiles: SelectedFile[] = [];
  selectedMode: 'individual' | 'combined' = 'individual';
  isConverting = false;
  isDragging = false;

  constructor(private convertService: ConvertService, private modal: ModalService) {}

  private readonly allowedExtensions = ['txt', 'doc', 'docx', 'odt', 'xls', 'xlsx', 'ods', 'ppt', 'pptx', 'odp', 'png', 'pdf', 'jpeg', 'jpg'];
  private readonly imageExtensions = ['png', 'jpeg', 'jpg'];
  private readonly maxFiles = 50;

  get totalSizeBytes(): number {
    return this.selectedFiles.reduce((s, item) => s + (item.file?.size || 0), 0);
  }

  get totalSizeMB(): number {
    return this.totalSizeBytes / 1024 / 1024;
  }

  get invalidFiles(): SelectedFile[] {
    return this.selectedFiles.filter(item => {
      const parts = item.file.name.split('.');
      const ext = parts.length > 1 ? parts.pop()!.toLowerCase() : '';
      return !this.allowedExtensions.includes(ext);
    });
  }

  get fileCount(): number {
    return this.selectedFiles.length;
  }

  getTruncatedFileName(name: string, maxLength: number = 25): string {
    if (name.length > maxLength) {
      return name.substring(0, maxLength - 3) + '...';
    }
    return name;
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files) {
      return;
    }
    this.addFiles(input.files);
  }

  addFiles(fileList: FileList) {
    let addedCount = 0;
    for (let i = 0; i < fileList.length; i++) {
      if (this.selectedFiles.length >= this.maxFiles) {
        const msg = `Limite de ${this.maxFiles} arquivos atingido. Não é possível adicionar mais.`;
        this.modal.show(msg, 'Limite de arquivos');
        this.errorMessage.emit(msg);
        break;
      }
      const file = fileList.item(i);
      if (file) {
        const parts = file.name.split('.');
        const ext = parts.length > 1 ? parts.pop()!.toLowerCase() : '';
        const isImage = this.imageExtensions.includes(ext);
        const preview = isImage ? URL.createObjectURL(file) : undefined;

        this.selectedFiles.push({ file, id: `${file.name}-${Date.now()}-${i}`, preview, isImage });
        addedCount++;
      }
    }
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
    if (event.dataTransfer?.files) {
      this.addFiles(event.dataTransfer.files);
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging = true;
  }

  removeFile(id: string) {
    const toRemove = this.selectedFiles.find(item => item.id === id);
    if (toRemove && toRemove.preview) {
      try { URL.revokeObjectURL(toRemove.preview); } catch {}
    }
    this.selectedFiles = this.selectedFiles.filter(item => item.id !== id);
  }

  clearFiles() {
    // revoke previews
    for (const item of this.selectedFiles) {
      if (item.preview) {
        try { URL.revokeObjectURL(item.preview); } catch {}
      }
    }
    this.selectedFiles = [];
    this.errorMessage.emit('');
  }

  drop(event: CdkDragDrop<any[]>) {
    moveItemInArray(
      this.selectedFiles,
      event.previousIndex,
      event.currentIndex
    );
  }

  async convert() {
    if (this.isConverting) {
      return;
    }

    if (!this.selectedFiles.length) {
      this.errorMessage.emit('Selecione ao menos um arquivo antes de converter.');
      return;
    }

    // Valida quantidade de arquivos (máximo 50)
    if (this.fileCount > this.maxFiles) {
      const msg = `Quantidade de arquivos (${this.fileCount}) excede o limite de ${this.maxFiles}.`;
      this.modal.show(msg, 'Limite de arquivos excedido');
      this.errorMessage.emit(msg);
      this.loadingState.emit(false);
      return;
    }

    // Valida formatos inválidos
    const invalid = this.invalidFiles;
    if (invalid.length) {
      const names = invalid.map(i => i.file.name).join(', ');
      const msg = `Existem formatos inválidos na lista: ${names}`;
      this.modal.show(msg, 'Formato inválido');
      // fallback: também emitir mensagem inline
      this.errorMessage.emit(msg);
      this.loadingState.emit(false);
      return;
    }

    // Valida limite total (500 MB)
    const maxBytes = 500 * 1024 * 1024;
    if (this.totalSizeBytes > maxBytes) {
      const msg = `Tamanho total dos arquivos selecionados (${this.totalSizeMB.toFixed(1)} MB) excede o limite de 500 MB.`;
      this.modal.show(msg, 'Limite excedido');
      // fallback: também emitir mensagem inline
      this.errorMessage.emit(msg);
      this.loadingState.emit(false);
      return;
    }

    this.isConverting = true;
    this.loadingState.emit(true);
    this.errorMessage.emit('');

    try {
      if (this.selectedMode === 'combined') {
        await this.convertCombined();
      } else {
        await this.convertIndividual();
      }
    } finally {
      this.isConverting = false;
      this.loadingState.emit(false);
    }
  }

  private async convertIndividual() {
    try {
      const results: PdfResult[] = [];
      for (const item of this.selectedFiles) {
        const blob = await firstValueFrom(this.convertService.uploadFile(item.file, false));
        const url = URL.createObjectURL(blob);
        results.push({ fileName: this.toPdfFileName(item.file.name), data: url });
      }
      this.pdfReady.emit(results);
    } catch (error) {
      this.handleError(error);
    }
  }

  private async convertCombined() {
    try {
      const blob = await firstValueFrom(this.convertService.uploadFiles(this.selectedFiles.map(item => item.file), true));
      const url = URL.createObjectURL(blob);
      this.pdfReady.emit([{ fileName: 'combined.pdf', data: url }]);
    } catch (error) {
      this.handleError(error);
    }
  }

  private handleError(error: any) {
    let message = 'Erro inesperado ao converter arquivo.';

    try {
      if (error) {
        if (error.error) {
          if (typeof error.error === 'string') {
            try {
              const parsed = JSON.parse(error.error);
              if (parsed && parsed.message) {
                message = parsed.message;
              } else {
                message = error.error;
              }
            } catch {
              message = error.error;
            }
          } else if (error.error.message) {
            message = error.error.message;
          }
        } else if (error.message) {
          message = error.message;
        }
      }
    } catch (e) {
      // fallback to generic message
    }

    // Mostrar modal estilizado
    try {
      this.modal.show(message, 'Erro');
    } catch { }

    this.errorMessage.emit(message);
    this.loadingState.emit(false);
  }

  private toPdfFileName(originalName: string) {
    const base = originalName.replace(/\.[^/.]+$/, '');
    return `${base}.pdf`;
  }
}
