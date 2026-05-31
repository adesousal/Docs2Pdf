import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { ModalService, ModalPayload } from '../../services/modal.service';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.css']
})
export class ModalComponent implements OnInit, OnDestroy {
  visible = false;
  title = 'Aviso';
  message = '';
  private sub?: Subscription;

  constructor(private modal: ModalService) {}

  ngOnInit(): void {
    this.sub = this.modal.onMessage().subscribe((p: ModalPayload | null) => {
      if (p) {
        this.title = p.title || 'Aviso';
        this.message = p.message;
        this.visible = true;
      } else {
        this.visible = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  close() {
    this.modal.close();
  }
}
