import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse, HttpHeaders } from '@angular/common/http'; // Adicionado HttpHeaders
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ConvertService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  uploadFile(file: File, combine = false): Observable<Blob> {
    const form = new FormData();
    form.append('files', file, file.name);
    const params = new HttpParams().set('combine', combine.toString());    

    return this.http.post(this.apiUrl, form, {
      params,      
      responseType: 'blob',
      observe: 'response'
    }).pipe(
      map((response: HttpResponse<Blob>) => response.body as Blob)
    );
  }

  uploadFiles(files: File[], combine = false): Observable<Blob> {
    const form = new FormData();
    files.forEach(file => form.append('files', file, file.name));
    const params = new HttpParams().set('combine', combine.toString());

    // Configura o cabeçalho para pular o aviso do Localtonet
    const headers = new HttpHeaders({
      'localtonet-skip-warning': 'true'
    });

    return this.http.post(this.apiUrl, form, {
      params,
      headers, // Passando os headers aqui
      responseType: 'blob',
      observe: 'response'
    }).pipe(
      map((response: HttpResponse<Blob>) => response.body as Blob)
    );
  }
}