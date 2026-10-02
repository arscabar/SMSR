// All content is fictional UI fixture data, not an analysis of an actual repository.
const previewItems = [
  {id:'order',label:'OrderService.cs',kind:'code',path:'src/Orders/OrderService.cs',
    role:'주문을 접수하고 결제 처리에 요청을 전달합니다.',
    evidence:'가상 원문에서 Create가 Charge를 호출합니다. 실제 분석 화면은 해당 위치의 근거를 연결합니다.',
    source:'// 가상 코드 예시\npublic Order Create(OrderRequest request) {\n    payment.Charge(request.Amount);\n    return repository.Save(request);\n}',
    related:[['payment','호출'],['design','설명 문서']]},
  {id:'payment',label:'PaymentClient.ts',kind:'code',path:'src/Payments/PaymentClient.ts',
    role:'결제 요청을 외부 서비스에 전달합니다.',evidence:'가상 코드에 결제 요청 함수가 있습니다. 외부 서비스 내부 동작은 확인되지 않았습니다.',
    source:'// 가상 코드 예시\nexport async function charge(amount: number) {\n  return gateway.send({ amount });\n}',related:[['order','이 코드에서 사용'],['design','설명 문서']]},
  {id:'design',label:'주문·결제 설계.md',kind:'document',path:'docs/order-payment.md',
    role:'주문 접수와 결제 요청의 책임을 나눈 이유를 설명합니다.',evidence:'가상 문서가 두 코드 파일을 명시적으로 참조합니다.',
    source:'# 주문·결제 설계 (가상 예시)\n\n주문 접수는 OrderService.cs, 결제 통신은 PaymentClient.ts에서 담당한다.\n결제 서비스 변경이 주문 규칙 변경으로 번지지 않도록 책임을 나눈다.',
    related:[['order','참조 코드'],['payment','참조 코드'],['image','참고 이미지']]},
  {id:'image',label:'결제 화면.png',kind:'image',path:'assets/payment.png',role:'결제 화면을 설명하는 참고 이미지입니다.',
    evidence:'내용 분석 여부와 파일 참조 관계는 구분해서 표시합니다.',source:'이미지 파일은 포함하지 않았습니다. 실제 앱에서는 이미지 미리보기를 유지합니다.',related:[['design','참조 문서']]},
  {id:'video',label:'사용 방법.mp4',kind:'video',path:'media/tutorial.mp4',role:null,evidence:'아직 내용 분석 결과가 없습니다.',source:'영상 파일은 포함하지 않았습니다. 실제 앱에서는 영상 재생을 유지합니다.',related:[]},
  {id:'audio',label:'회의 기록.mp3',kind:'audio',path:'media/meeting.mp3',role:null,evidence:'아직 전사 결과가 없습니다.',source:'음원 파일은 포함하지 않았습니다. 실제 앱에서는 음원 재생을 유지합니다.',related:[]}
];
const previewKindNames = {code:'코드',document:'문서',image:'이미지',video:'영상',audio:'음원'};
