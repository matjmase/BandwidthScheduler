import { Component, Input, TemplateRef, ViewChild } from '@angular/core';
import { IMessageModalBoxModel } from './message-modal-box-model';
import { ModalDismissReasons, NgbModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-message-modal-box',
  templateUrl: './message-modal-box.component.html',
  styleUrl: './message-modal-box.component.scss',
})
export class MessageModalBoxComponent {
  @Input() Model: IMessageModalBoxModel | undefined;

  @ViewChild('content') content!: TemplateRef<any>;

  constructor(private modalService: NgbModal) {}

  public ShowModal(): Promise<boolean> {
    return this.open(this.content);
  }

  private open(content: TemplateRef<any>): Promise<boolean> {
    return this.modalService
      .open(content, {
        ariaLabelledBy: 'modal-basic-title',
      })
      .result.catch((e) => this.getDismissReason(e));
  }

  private getDismissReason(reason: any): boolean {
    switch (reason) {
      case ModalDismissReasons.ESC:
        return false;
      case ModalDismissReasons.BACKDROP_CLICK:
        return false;
      default:
        return !!reason;
    }
  }
}
