import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MessageModalBoxComponent } from './message-modal-box.component';

describe('MessageModalBoxComponent', () => {
  let component: MessageModalBoxComponent;
  let fixture: ComponentFixture<MessageModalBoxComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MessageModalBoxComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(MessageModalBoxComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
