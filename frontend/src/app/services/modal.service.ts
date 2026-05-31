import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';

export interface ModalPayload {
  title?: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ModalService {
  private subject = new Subject<ModalPayload | null>();

  show(message: string, title?: string) {
    this.subject.next({ message, title });
  }

  close() {
    this.subject.next(null);
  }

  onMessage(): Observable<ModalPayload | null> {
    return this.subject.asObservable();
  }
}
